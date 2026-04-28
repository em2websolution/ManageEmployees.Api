namespace ManageEmployees.Domain.DTO
{
    public class UserDto
    {
        public string UserId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DocNumber { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = null!;
    }
}
