using CompliCore.Enums;
using CompliCore.Interfaces;

namespace CompliCore.Models
{

    /// <summary>
    /// auditLog is the append-only history of who changed what. ChangesJson holds the old and new values.
    /// UserEmail is a snapshot so history survives a user being deleted.
    /// EntityId has no foreign key, because the row it points to may be gone.
    /// </summary>
    public class AuditLog : ITenantEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public Guid? UserId { get; set; }
        public string? UserEmail { get; set; }
        public string EntityName { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public AuditAction Action { get; set; } 
        public string ChangesJson { get; set; } = "{}";

        public DateTime Timestamp { get; set; }

    }
}
