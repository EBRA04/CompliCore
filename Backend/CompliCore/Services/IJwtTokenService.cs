using CompliCore.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CompliCore.Services;

public record TokenResult(string Token, DateTime ExpiresAt);

public interface IJwtTokenService
{
    TokenResult GenerateAccessToken(User user);
}

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public TokenResult GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var claims = new[]
         {
         new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
         new Claim(ClaimTypes.Email, user.Email),
         new Claim(ClaimTypes.Name, user.FullName),
         new Claim(ClaimTypes.Role, user.Role.ToString()),
         new Claim(TenantClaimTypes.TenantId, user.TenantId.ToString()),
         };

        var expiresAt = DateTime.UtcNow.AddMinutes(
            int.Parse(_configuration["Jwt:AccessTokenExpiryMinutes"]!));

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(
                key, SecurityAlgorithms.HmacSha256)
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new TokenResult(tokenString, expiresAt);
    }
}