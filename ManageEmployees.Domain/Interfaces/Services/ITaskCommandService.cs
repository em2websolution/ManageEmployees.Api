using ManageEmployees.Domain.DTO;
using ManageEmployees.Domain.Entities;

namespace ManageEmployees.Domain.Interfaces.Services;

public interface ITaskCommandService
{
    Task<TaskItem> CreateAsync(CreateTaskRequest request);
    Task<TaskItem> UpdateAsync(Guid id, UpdateTaskRequest request);
    Task<bool> DeleteAsync(Guid id);
}
