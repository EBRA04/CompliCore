using System.ComponentModel.DataAnnotations;

namespace CompliCore.DTOs.UserDtos;

public record CreateUserRequest(
    [Required] string FullName,
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required] CompliCore.Enums.UserRole Role
);

public record UserListItemResponse(Guid Id, string FullName, string Email, string Role, DateTime CreatedAt);