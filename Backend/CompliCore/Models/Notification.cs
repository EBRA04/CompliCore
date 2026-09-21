using CompliCore.Interfaces;

namespace CompliCore.Models
{
    /// <summary>
    /// Notification is an in-app reminder created by the worker. Threshold is 60, 30, 7, or 0 (expired). 
    /// ExpiryDateSnapshot is the expiry date at creation time. Together with the item and threshold it forms a unique key,
    /// so running the worker twice creates nothing new, 
    /// and renewing restarts the cycle. ReadAt == null means unread.
    /// </summary>
    public class Notification : ITenantEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public Guid ComplianceItemId { get; set; }
        public int Threshold { get; set; }
        public DateOnly ExpiryDateSnapshot { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }

    }
}
