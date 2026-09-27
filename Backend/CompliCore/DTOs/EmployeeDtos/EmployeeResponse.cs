namespace CompliCore.DTOs.EmployeeDtos;

public record EmployeeResponse(
    Guid Id,
    string FullName,
    string Nationality,
    string? IqamaNumber,
    string? JobTitle,
    DateTime CreatedAt,
    DateTime UpdatedAt
);