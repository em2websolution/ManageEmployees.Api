using ManageEmployees.Domain.DTO;

namespace ManageEmployees.Domain.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<List<UserDto>> GetAllWithRolesAsync();
    }
}
