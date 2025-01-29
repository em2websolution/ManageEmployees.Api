using Microsoft.AspNetCore.Identity;

namespace ManageEmployees.Domain.Entities
{
    public class User : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty; 

        public string LastName { get; set; } = string.Empty; 

        public string DocNumber { get; set; } = string.Empty; 

        public string? ManagerId { get; set; }
    }
}
