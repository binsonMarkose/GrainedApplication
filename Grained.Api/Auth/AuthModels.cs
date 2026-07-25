namespace Grained.Api.Auth;

public record LoginRequest(string Email, string Password);

public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Email, string Token, string Password);

// Signed-in self-service account edits.
public record UpdateProfileRequest(string FullName, string Email);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
// First-run: set a password when forced to (no current password needed — identity is proven by the JWT).
public record SetPasswordRequest(string NewPassword);

public record UserDto(Guid Id, string Email, string FullName, Guid? ChurchId, IReadOnlyList<string> Roles, bool MustChangePassword);

public record LoginResponse(string Token, DateTime ExpiresAtUtc, UserDto User);
