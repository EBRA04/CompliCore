using CompliCore.Enums;
using CompliCore.Interfaces;

namespace CompliCore.Models
{

    /// <summary>
    /// ComplianceItem is the heart of the app: one thing that expires. EmployeeId is null for company-level items
    ///  It also has Type, Title, an optional ReferenceNumber,
    /// an optional IssueDate, and a required ExpiryDate that drives status and reminders. 
    /// Renewing means updating the ExpiryDate.
    /// </summary>
    public class ComplianceItem : ITenantEntity, ITimestamped
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public Guid? EmployeeId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public ComplianceItemType Type { get; set; }
        public string? ReferenceNumber { get; set; }
        public DateOnly? IssueDate { get; set; }
        public DateOnly ExpiryDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Employee? Employee { get; set; }
        public ICollection<Document> Documents { get; set; } = new List<Document>();

    }
}
