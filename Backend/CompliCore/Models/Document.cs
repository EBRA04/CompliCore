using CompliCore.Interfaces;

namespace CompliCore.Models
{

    /// <summary>
    /// Document is metadata about an uploaded file. The file itself lives on disk (the Docker volume), and the row stores where.
    /// OriginalFileName is display only. StoredFileName is server-generated ({Id:N}{ext}), so a client can never control a path.
    /// </summary>
    public class Document : ITenantEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public Guid ComplianceItemId { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public Guid? UploadedByUserId { get; set; }

        public DateTime UploadedAt { get; set; }
    }
}
