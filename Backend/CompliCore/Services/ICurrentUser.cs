using System.Security.Claims;

namespace CompliCore.Services;

public static class TenantClaimTypes
{
    public const string TenantId = "tenant_id";
}

public interface ICurrentUser
{
    Guid TenantId { get; }
    Guid? UserId { get; }
    string? Email { get; }
    string? Role { get; }
}

public class CurrentUser : ICurrentUser
{
    private readonly ClaimsPrincipal? _user;

    public CurrentUser(IHttpContextAccessor accessor)
        => _user = accessor.HttpContext?.User;

    public Guid TenantId => Guid.TryParse(_user?.FindFirstValue(TenantClaimTypes.TenantId), out var id) ? id : Guid.Empty;

    public Guid? UserId => Guid.TryParse(_user?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string? Email => _user?.FindFirstValue(ClaimTypes.Email);

    public string? Role => _user?.FindFirstValue(ClaimTypes.Role);
}