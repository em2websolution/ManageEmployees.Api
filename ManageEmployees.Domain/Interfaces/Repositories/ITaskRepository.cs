using ManageEmployees.Domain.Entities;
using ManageEmployees.Domain.Models;

namespace ManageEmployees.Domain.Interfaces.Repositories
{
    public interface ITaskRepository
    {
        Task<PagedResult<TaskItem>> GetAllAsync(int page, int pageSize);
        Task<TaskItem?> GetByIdAsync(Guid id);
        Task<List<TaskItem>> GetByUserIdAsync(string userId);
        Task CreateAsync(TaskItem task);
        Task UpdateAsync(TaskItem task);
        Task<bool> DeleteAsync(Guid id);
    }
}
