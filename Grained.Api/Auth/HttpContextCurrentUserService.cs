using System.Security.Claims;
using Grained.Application.Common.Exceptions;
using Grained.Application.Common.Interfaces;
using Grained.Domain.Common;
using Grained.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;

namespace Grained.Api.Auth;

// API counterpart of the Blazor CurrentUserService: reads the same claims, but from the
// JWT-authenticated HttpContext.User rather than a Blazor AuthenticationStateProvider.
// Application services and their church-isolation checks are unchanged.
public class HttpContextCurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public Guid? UserId => ParseGuid(Principal?.FindFirstValue(ClaimTypes.NameIdentifier));

    public Guid? ChurchId => ParseGuid(Principal?.FindFirstValue(ApplicationUserClaimsPrincipalFactory.ChurchIdClaimType));

    public bool IsSuperAdmin => Principal?.IsInRole(Roles.SuperAdmin) ?? false;

    public bool IsChurchAdmin => Principal?.IsInRole(Roles.ChurchAdmin) ?? false;

    public bool IsTeacher => Principal?.IsInRole(Roles.Teacher) ?? false;

    public bool IsParent => Principal?.IsInRole(Roles.Parent) ?? false;

    public Guid RequireChurchId() =>
        ChurchId ?? throw new ValidationException("No church is associated with the current user.");

    private static Guid? ParseGuid(string? value) => Guid.TryParse(value, out var id) ? id : null;
}
