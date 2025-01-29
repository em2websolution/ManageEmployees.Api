namespace ManageEmployees.Domain.DTO
{
    public class UserWithManagerDto
    {
        public string UserId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DocNumber { get; set; } = string.Empty;
        public string? ManagerId { get; set; }
        public string? ManagerName { get; set; }
        public List<string> PhoneNumbers { get; set; } = new List<string>();
        public string Role { get; set; } = null!;
    }
}
