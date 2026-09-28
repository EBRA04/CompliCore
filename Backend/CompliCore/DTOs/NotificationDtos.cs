namespace CompliCore.DTOs;

public record NotificationResponse(
    Guid Id,
    Guid ComplianceItemId,
    int Threshold,
    string Message,
    DateTime CreatedAt,
    DateTime? ReadAt
);