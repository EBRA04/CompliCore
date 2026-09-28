namespace CompliCore.DTOs.AuditLogDtos;

public record AuditLogResponse(
    Guid Id,
    Guid? UserId,
    string? UserEmail,
    string EntityName,
    Guid EntityId,
    string Action,
    string ChangesJson,
    DateTime Timestamp
);