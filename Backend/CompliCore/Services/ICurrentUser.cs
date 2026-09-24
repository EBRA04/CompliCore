using System.Security.Claims;   // ClaimsPrincipal, ClaimTypes — used below

namespace CompliCore.Services;

// Single source of truth for the "tenant_id" claim NAME. Written here AND
// read from JwtTokenService via this same constant — if it were a raw
// string typed in both files, a typo in either place would silently break
// tenant isolation (FindFirstValue would just return null forever, no
// compile error). Using the constant turns a typo into a compile error.
public static class TenantClaimTypes
{
    public const string TenantId = "tenant_id";
}

// The CONTRACT: "anything that answers 'who is asking right now' must
// expose these four read-only properties." No setters — this is looked
// up/derived, never assigned from outside.
// Other services depend on THIS interface, not on CurrentUser directly —
// that's what lets tests later swap in a fake implementation with no
// real HttpContext.
public interface ICurrentUser
{
    Guid TenantId { get; }     // never null — Guid.Empty is the "no tenant" value
    Guid? UserId { get; }      // null = genuinely no logged-in user
    string? Email { get; }
    string? Role { get; }
}

// THE real implementation — reads claims the JWT middleware already
// validated and parsed onto HttpContext.User, before this class ever runs.
public class CurrentUser : ICurrentUser
{
    // readonly: set once in the constructor, never reassigned.
    // Nullable: HttpContext can be null (e.g. a background job later,
    // outside any real web request) — must handle that safely, not crash.
    //ClaimsPrincipal is ASP.NET Core's representation of "who is making this request,"
    private readonly ClaimsPrincipal? _user;

    public CurrentUser(IHttpContextAccessor accessor)
        // ?. = null-conditional: if HttpContext is null, short-circuits to
        // null instead of throwing. IHttpContextAccessor is how a
        // DI-created class (not a controller) reaches the current request.
        => _user = accessor.HttpContext?.User;

    // TryParse (not Parse) = safe: returns false instead of throwing on
    // bad/missing input. On failure -> Guid.Empty, a REAL Guid that will
    // never match a real tenant (tenant ids are random Guids), so
    // `WHERE TenantId == CurrentTenantId` later matches ZERO rows instead
    // of crashing or leaking data. This is the "safe by default" design.
    public Guid TenantId => Guid.TryParse(_user?.FindFirstValue(TenantClaimTypes.TenantId), out var id) ? id : Guid.Empty;

    // ClaimTypes.NameIdentifier = a BUILT-IN .NET constant (not a custom
    // string like tenant_id), because "user id" is a standard claim type.
    // Falls back to null, not Guid.Empty — there's no security-filter use
    // case for UserId the way there is for TenantId, so null ("no user")
    // is the honest answer, and callers must handle it explicitly.
    public Guid? UserId => Guid.TryParse(_user?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    // Already strings -> no TryParse needed. Same ?. null-safety:
    // no context or no such claim -> null.
    public string? Email => _user?.FindFirstValue(ClaimTypes.Email);

    public string? Role => _user?.FindFirstValue(ClaimTypes.Role);
}