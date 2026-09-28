using System.ComponentModel.DataAnnotations;
using CompliCore.Enums;

namespace CompliCore.DTOs.ComplianceItemDtos;

public record CreateComplianceItemRequest(
    Guid? EmployeeId,
    [Required] ComplianceItemType Type,
    string? Title,
    string? ReferenceNumber,
    DateOnly? IssueDate,
    [Required] DateOnly ExpiryDate,
    string? Notes
);

public record UpdateComplianceItemRequest(
    Guid? EmployeeId,
    [Required] ComplianceItemType Type,
    string? Title,
    string? ReferenceNumber,
    DateOnly? IssueDate,
    [Required] DateOnly ExpiryDate,
    string? Notes
);