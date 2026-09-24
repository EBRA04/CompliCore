namespace CompliCore.DTOs.AuthDtos
{
    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }

        public UserSummary User { get; set; } = null!;
    }
    public class UserSummary
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public Guid TenantId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
    }
}
