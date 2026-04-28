using ManageEmployees.Domain.Entities;

namespace ManageEmployees.Domain.Interfaces.Repositories
{
    public interface ITaskRepository
    {
        Task<List<TaskItem>> GetAllAsync();
        Task<TaskItem?> GetByIdAsync(Guid id);
        Task<List<TaskItem>> GetByUserIdAsync(string userId);
        Task CreateAsync(TaskItem task);
        Task UpdateAsync(TaskItem task);
        Task<bool> DeleteAsync(Guid id);
    }
}
