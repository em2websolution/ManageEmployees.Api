using ManageEmployees.Domain.Entities;
using ManageEmployees.Domain.Models;

namespace ManageEmployees.Domain.Interfaces.Services;

public interface ITaskQueryService
{
    Task<PagedResult<TaskItem>> GetAllAsync(int page, int pageSize, string? search = null, string? status = null, DateTime? startDate = null, DateTime? endDate = null);
    Task<TaskItem?> GetByIdAsync(Guid id);
}
