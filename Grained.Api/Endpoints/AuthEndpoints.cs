using System.Security.Claims;
using Grained.Api.Auth;
using Grained.Application.Auth;
using Grained.Domain.Entities;
using Grained.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;

namespace Grained.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", async (
            LoginRequest req,
            UserManager<ApplicationUser> userManager,
            JwtTokenService tokens) =>
        {
            var user = await userManager.FindByEmailAsync(req.Email);
            if (user is null || !user.IsActive)
                return Results.Unauthorized();

            if (!await userManager.CheckPasswordAsync(user, req.Password))
                return Results.Unauthorized();

            var roles = await userManager.GetRolesAsync(user);
            var (token, expires) = tokens.Create(user, roles);

            var dto = new UserDto(user.Id, user.Email ?? "", user.FullName, user.ChurchId, roles.ToList(), user.MustChangePassword);
            return Results.Ok(new LoginResponse(token, expires, dto));
        });

        group.MapGet("/me", (ClaimsPrincipal principal) =>
        {
            var id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(id, out var userId))
                return Results.Unauthorized();

            var churchId = principal.FindFirstValue(ApplicationUserClaimsPrincipalFactory.ChurchIdClaimType);
            var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

            var dto = new UserDto(
                userId,
                principal.FindFirstValue(ClaimTypes.Email) ?? "",
                principal.FindFirstValue(ApplicationUserClaimsPrincipalFactory.FullNameClaimType) ?? "",
                Guid.TryParse(churchId, out var cid) ? cid : null,
                roles,
                principal.FindFirstValue("must_change_password") == "true");

            return Results.Ok(dto);
        }).RequireAuthorization();

        // Update the signed-in user's own name / email. Re-issues the token so the JWT claims (which
        // /me reads) stay in sync with the new details.
        group.MapPut("/me", async (
            UpdateProfileRequest req,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            JwtTokenService tokens) =>
        {
            if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Results.Unauthorized();
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                return Results.Unauthorized();

            var fullName = (req.FullName ?? "").Trim();
            var email = (req.Email ?? "").Trim();
            if (fullName.Length == 0)
                return Results.BadRequest(new { message = "Your name is required." });
            if (email.Length == 0)
                return Results.BadRequest(new { message = "An email address is required." });

            user.FullName = fullName;

            if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                var existing = await userManager.FindByEmailAsync(email);
                if (existing is not null && existing.Id != user.Id)
                    return Results.BadRequest(new { message = "That email is already in use by another account." });

                var e1 = await userManager.SetEmailAsync(user, email);
                var e2 = await userManager.SetUserNameAsync(user, email); // login is by email
                if (!e1.Succeeded || !e2.Succeeded)
                    return Results.BadRequest(new { message = string.Join(" ", e1.Errors.Concat(e2.Errors).Select(x => x.Description)) });
            }
            else
            {
                var upd = await userManager.UpdateAsync(user);
                if (!upd.Succeeded)
                    return Results.BadRequest(new { message = string.Join(" ", upd.Errors.Select(x => x.Description)) });
            }

            var roles = await userManager.GetRolesAsync(user);
            var (token, expires) = tokens.Create(user, roles);
            var dto = new UserDto(user.Id, user.Email ?? "", user.FullName, user.ChurchId, roles.ToList(), user.MustChangePassword);
            return Results.Ok(new LoginResponse(token, expires, dto));
        }).RequireAuthorization();

        // Change the signed-in user's password (requires the current one).
        group.MapPost("/change-password", async (
            ChangePasswordRequest req,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager) =>
        {
            if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Results.Unauthorized();
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                return Results.Unauthorized();

            var result = await userManager.ChangePasswordAsync(user, req.CurrentPassword ?? "", req.NewPassword ?? "");
            if (!result.Succeeded)
            {
                var wrongCurrent = result.Errors.Any(e => e.Code == "PasswordMismatch");
                var message = wrongCurrent
                    ? "Your current password is incorrect."
                    : string.Join(" ", result.Errors.Select(e => e.Description));
                return Results.BadRequest(new { message });
            }
            return Results.Ok(new { message = "Your password has been updated." });
        }).RequireAuthorization();

        // First sign-in: set your own password to replace the admin-issued login code. No current
        // password required (identity is proven by the authenticated JWT); only allowed while the
        // "must change password" flag is set. Clears the flag and re-issues a token.
        group.MapPost("/set-password", async (
            SetPasswordRequest req,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            JwtTokenService tokens) =>
        {
            if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Results.Unauthorized();
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                return Results.Unauthorized();
            if (!user.MustChangePassword)
                return Results.BadRequest(new { message = "Use the change-password form to update your password." });

            var newPassword = req.NewPassword ?? "";
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var reset = await userManager.ResetPasswordAsync(user, token, newPassword);
            if (!reset.Succeeded)
                return Results.BadRequest(new { message = string.Join(" ", reset.Errors.Select(e => e.Description)) });

            user.MustChangePassword = false;
            await userManager.UpdateAsync(user);

            var roles = await userManager.GetRolesAsync(user);
            var (jwt, expires) = tokens.Create(user, roles);
            var dto = new UserDto(user.Id, user.Email ?? "", user.FullName, user.ChurchId, roles.ToList(), user.MustChangePassword);
            return Results.Ok(new LoginResponse(jwt, expires, dto));
        }).RequireAuthorization();

        // Request a reset link. Always 200 — never reveal whether the email is registered.
        group.MapPost("/forgot-password", async (
            ForgotPasswordRequest req,
            UserManager<ApplicationUser> userManager,
            IPasswordResetEmailSender email,
            IConfiguration config,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            var address = (req.Email ?? "").Trim();
            string? resetUrl = null;

            var user = await userManager.FindByEmailAsync(address);
            if (user is not null && user.IsActive)
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                resetUrl = BuildResetUrl(config, address, token);
                await email.SendPasswordResetAsync(address, resetUrl, ct);
            }

            return Results.Ok(new
            {
                message = "If that email is registered, a password reset link is on its way.",
                // Dev-only convenience so you can test without an email provider configured.
                resetUrl = env.IsDevelopment() ? resetUrl : null,
            });
        }).AllowAnonymous().RequireRateLimiting("invite");

        // Complete the reset with the emailed token.
        group.MapPost("/reset-password", async (
            ResetPasswordRequest req,
            UserManager<ApplicationUser> userManager) =>
        {
            const string generic = "This reset link is invalid or has expired. Please request a new one.";

            var user = await userManager.FindByEmailAsync((req.Email ?? "").Trim());
            if (user is null)
                return Results.BadRequest(new { message = generic });

            var result = await userManager.ResetPasswordAsync(user, req.Token, req.Password);
            if (!result.Succeeded)
            {
                var invalidToken = result.Errors.Any(e => e.Code.Contains("Token", StringComparison.OrdinalIgnoreCase));
                var message = invalidToken ? generic : string.Join(" ", result.Errors.Select(e => e.Description));
                return Results.BadRequest(new { message });
            }

            return Results.Ok(new { message = "Your password has been updated. You can now sign in." });
        }).AllowAnonymous().RequireRateLimiting("invite");
    }

    private static string BuildResetUrl(IConfiguration config, string email, string token)
    {
        var baseUrl = (config["App:WebBaseUrl"] ?? "http://localhost:5173").TrimEnd('/');
        return $"{baseUrl}/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
    }
}
