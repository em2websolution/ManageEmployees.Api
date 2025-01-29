using System.ComponentModel.DataAnnotations;

namespace ManageEmployees.Domain.DTO
{
    public class SignInRequest
    {
        [Required]
        public string UserName { get; set; } = null!;

        [Required, MinLength(6, ErrorMessage = "Please enter at least 6 characters!")]
        public string Password { get; set; } = null!;
    }
}
