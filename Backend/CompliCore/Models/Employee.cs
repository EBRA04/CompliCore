using CompliCore.Interfaces;

namespace CompliCore.Models
{

    /// <summary>
    /// Employee is a person at the company: name, nationality, optional 10-digit iqama number (unique per company),
    /// and optional job title. It has a list of their ComplianceItems.
    /// </summary>
    public class Employee : ITenantEntity, ITimestamped
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Nationality { get; set; } = string.Empty;
        public string? IqamaNumber { get; set; }
        public string? JobTitle { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public ICollection<ComplianceItem> ComplianceItems { get; set; } = new List<ComplianceItem>();
    }
}
