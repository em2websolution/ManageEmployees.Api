using System.ComponentModel.DataAnnotations;

namespace ManageEmployees.Domain.DTO
{
    public class CreateUser
    {
        [Required, MaxLength(256, ErrorMessage = "First Name cannot exceed 256 characters.")]
        public string FirstName { get; set; } = null!;

        [Required, MaxLength(256, ErrorMessage = "Last Name cannot exceed 256 characters.")]
        public string LastName { get; set; } = null!;

        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
        [MaxLength(64, ErrorMessage = "Password cannot exceed 64 characters.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,64}$", ErrorMessage = "Password must have at least one uppercase letter, one lowercase letter, one number, and one special character.")]
        public string Password { get; set; } = null!;

        [Required, Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = null!;

        [Required, MaxLength(256, ErrorMessage = "Document Number cannot exceed 256 characters.")]
        public string DocNumber { get; set; } = null!;

        [StringLength(450, ErrorMessage = "Manager ID must be a valid identifier.")]
        public string? ManagerId { get; set; }

        [Required(ErrorMessage = "Role is required.")]
        public string Role { get; set; } = null!;
        public string? PhoneNumber { get; set; }
    }
}
