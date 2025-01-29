namespace ManageEmployees.Domain.Entities
{
    public class RefreshToken
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = null!;
        public string Token { get; set; } = null!;
        public DateTime ExpireDate { get; set; }
    }
}
