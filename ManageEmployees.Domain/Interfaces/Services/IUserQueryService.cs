using ManageEmployees.Domain.DTO;
using ManageEmployees.Domain.Models;

namespace ManageEmployees.Domain.Interfaces.Services;

public interface IUserQueryService
{
    Task<PagedResult<UserDto>> GetAllUsersAsync(int page, int pageSize, string? search = null, string? role = null);
}
