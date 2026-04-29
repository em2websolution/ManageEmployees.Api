using ManageEmployees.Domain.DTO;
using ManageEmployees.Domain.Models;

namespace ManageEmployees.Domain.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<PagedResult<UserDto>> GetAllWithRolesAsync(int page, int pageSize, string? search = null, string? role = null);
    }
}
