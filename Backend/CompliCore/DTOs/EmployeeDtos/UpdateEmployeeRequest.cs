using System.ComponentModel.DataAnnotations;

namespace CompliCore.DTOs.EmployeeDtos
{
    public record UpdateEmployeeRequest(
     [Required] string FullName,
     [Required] string Nationality,
     string? IqamaNumber,
     string? JobTitle
 );
}
