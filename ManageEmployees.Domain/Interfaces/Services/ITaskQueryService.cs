using ManageEmployees.Domain.Entities;
using ManageEmployees.Domain.Models;

namespace ManageEmployees.Domain.Interfaces.Services;

public interface ITaskQueryService
{
    Task<PagedResult<TaskItem>> GetAllAsync(int page, int pageSize);
    Task<TaskItem?> GetByIdAsync(Guid id);
}
