using ManageEmployees.Domain.DTO;

namespace ManageEmployees.Domain.Interfaces.Services;

public interface IUserQueryService
{
    Task<List<UserDto>> GetAllUsersAsync();
}
