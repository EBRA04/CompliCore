using CompliCore.Models;                  // User entity, used in the method signature
using Microsoft.IdentityModel.Tokens;     // SymmetricSecurityKey, SigningCredentials
using System.IdentityModel.Tokens.Jwt;    // JwtSecurityToken, JwtSecurityTokenHandler
using System.Security.Claims;             // Claim, ClaimTypes
using System.Text;                        // Encoding, to turn the key string into bytes

namespace CompliCore.Services;

// A `record` = built for immutable data-carriers: this one line generates
// the constructor, properties, equality and ToString() for us.
// Needed because GenerateAccessToken must return TWO things (the token
// string AND when it expires), not just one.
public record TokenResult(string Token, DateTime ExpiresAt);

// The CONTRACT: one method — given a User, produce a signed token result.
// This is a METHOD (an action/verb), unlike ICurrentUser's properties
// (passive lookups) — building+signing a new token is genuinely "doing
// something" each call, not just reading existing state.
public interface IJwtTokenService
{
    TokenResult GenerateAccessToken(User user);
}

public class JwtTokenService : IJwtTokenService
{
    // IConfiguration = reads settings from WHICHEVER layer actually has
    // them: appsettings.json, appsettings.Development.json, user-secrets
    // (Development only), or env vars (Jwt__Key in Docker/compose) — same
    // key ("Jwt:Key") resolves correctly regardless of source.
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public TokenResult GenerateAccessToken(User user)
    {
        // Signing needs raw bytes, not a C# string -> UTF8.GetBytes.
        // The trailing ! (null-forgiving operator) tells the compiler
        // "trust me, don't warn" — Jwt:Key is required config; if it's
        // genuinely missing we WANT a loud crash here, not silent garbage.
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

        // THE WRITE SIDE — mirrors exactly what CurrentUser reads on the
        // other end. Every claim name here has a matching FindFirstValue
        // call in CurrentUser.cs.
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),   // <- CurrentUser.UserId reads this
            new Claim(ClaimTypes.Email, user.Email),                    // <- CurrentUser.Email
            new Claim(ClaimTypes.Name, user.FullName),                  // spec 8.3 requires it; not read by CurrentUser
            new Claim(ClaimTypes.Role, user.Role.ToString()),           // enum -> string; a claim can only hold text
            new Claim(TenantClaimTypes.TenantId, user.TenantId.ToString()), // <- CurrentUser.TenantId reads this. THE key claim.
        };

        // Computed ONCE, reused below in both the token itself and the
        // returned TokenResult. If computed twice (two separate
        // DateTime.UtcNow calls), the two values could differ by
        // milliseconds and disagree with each other.
        var expiresAt = DateTime.UtcNow.AddMinutes(
            int.Parse(_configuration["Jwt:AccessTokenExpiryMinutes"]!));

        // Builds the token OBJECT (not yet the final string).
        // issuer/audience = anti-forgery: lets the validator later reject
        // a token that's correctly signed but meant for a different app.
        // expires = baked in; middleware checks this automatically later,
        // no manual expiry check needed anywhere else.
        // SigningCredentials = the cryptographic signature; tampering with
        // ANY part of the payload (e.g. editing tenant_id) invalidates it.
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(
                key, SecurityAlgorithms.HmacSha256)
        );

        // Serializes the token OBJECT into the actual compact JWT string
        // (header.payload.signature, base64) — this is what gets sent
        // to the client and stored by it.
        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        // Bundles both pieces so AuthService can build its response
        // without recomputing the expiry separately.
        return new TokenResult(tokenString, expiresAt);
    }
}