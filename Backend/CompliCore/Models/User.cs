using CompliCore.Enums;
using CompliCore.Interfaces;
namespace CompliCore.Models
{
    /// <summary>
    /// User is a login.
    /// It holds a BCrypt PasswordHash (never returned to clients, never audited) and a Role.
    /// The email is stored lowercase and is the only globally unique value, because at login we don't know the tenant yet.
    /// </summary>
    public class User : ITenantEntity,ITimestamped
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

    }
}
