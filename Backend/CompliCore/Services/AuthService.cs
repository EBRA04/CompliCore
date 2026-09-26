using CompliCore.Data;
using CompliCore.DTOs.AuthDtos;
using CompliCore.Enums;
using CompliCore.Exceptions;
using CompliCore.Models;
using Microsoft.EntityFrameworkCore;

namespace CompliCore.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly TimeProvider _time;

    public AuthService(AppDbContext db, IJwtTokenService jwtTokenService, TimeProvider time)
    {
        _db = db;
        _jwtTokenService = jwtTokenService;
        _time = time;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var normalizedEmail = request.Email.ToLowerInvariant();

        if (await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == normalizedEmail))
        {
            throw new ConflictException("User with this email already exists.");
        }

        var tenant = new Tenant
        {
            Name = request.CompanyName,
            CreatedAt = _time.GetUtcNow().UtcDateTime
        };
        _db.Tenants.Add(tenant);

        var user = new User
        {
            TenantId = tenant.Id,
            FullName = request.FullName,
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = UserRole.Admin
        };
        _db.Users.Add(user);

        await _db.SaveChangesAsync();

        var tokenResult = _jwtTokenService.GenerateAccessToken(user);

        return new AuthResponse
        {
            Token = tokenResult.Token,
            ExpiresAt = tokenResult.ExpiresAt,
            User = new UserSummary
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                TenantId = user.TenantId,
                CompanyName = tenant.Name
            }
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var normalizedEmail = request.Email.ToLowerInvariant();

        var user = await _db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        var tenant = await _db.Tenants
            .IgnoreQueryFilters()
            .FirstAsync(t => t.Id == user.TenantId);

        var tokenResult = _jwtTokenService.GenerateAccessToken(user);

        return new AuthResponse
        {
            Token = tokenResult.Token,
            ExpiresAt = tokenResult.ExpiresAt,
            User = new UserSummary
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                TenantId = user.TenantId,
                CompanyName = tenant.Name
            }
        };
    }

        public async Task<UserSummary> GetCurrentUserAsync(Guid userId)
            {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            throw new NotFoundException("User not found.");
        }

        var tenant = await _db.Tenants.FirstAsync(t => t.Id == user.TenantId);

        return new UserSummary
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString(),
            TenantId = user.TenantId,
            CompanyName = tenant.Name
        };
}
}