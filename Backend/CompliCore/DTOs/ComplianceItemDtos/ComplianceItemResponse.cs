namespace CompliCore.DTOs.ComplianceItemDtos;

public record ComplianceItemResponse(
    Guid Id,
    Guid? EmployeeId,
    string Type,
    string Title,
    string? ReferenceNumber,
    DateOnly? IssueDate,
    DateOnly ExpiryDate,
    string? Notes,
    string Status,
    int DaysRemaining,
    DateTime CreatedAt,
    DateTime UpdatedAt
);