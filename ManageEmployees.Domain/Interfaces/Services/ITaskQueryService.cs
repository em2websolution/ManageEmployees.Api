using ManageEmployees.Domain.Entities;

namespace ManageEmployees.Domain.Interfaces.Services;

public interface ITaskQueryService
{
    Task<List<TaskItem>> GetAllAsync();
    Task<TaskItem?> GetByIdAsync(Guid id);
}
