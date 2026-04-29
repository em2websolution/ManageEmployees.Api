using ManageEmployees.Domain;
using ManageEmployees.Domain.DTO;
using ManageEmployees.Domain.Entities;
using ManageEmployees.Domain.Exceptions;
using ManageEmployees.Domain.Interfaces.Repositories;
using ManageEmployees.Domain.Interfaces.Services;
using ManageEmployees.Domain.Models;
using Microsoft.Extensions.Logging;

namespace ManageEmployees.Services.Services
{
    public class TaskService : ITaskQueryService, ITaskCommandService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly ILogger<TaskService> _logger;

        public TaskService(ITaskRepository taskRepository, ILogger<TaskService> logger)
        {
            _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PagedResult<TaskItem>> GetAllAsync(int page, int pageSize, string? search = null, string? status = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            return await _taskRepository.GetAllAsync(page, pageSize, search, status, startDate, endDate);
        }

        public async Task<TaskItem?> GetByIdAsync(Guid id)
        {
            return await _taskRepository.GetByIdAsync(id);
        }

        public async Task<TaskItem> CreateAsync(CreateTaskRequest request)
        {
            ValidateStatus(request.Status);

            var task = new TaskItem
            {
                Title = request.Title,
                Description = request.Description,
                Status = request.Status,
                DueDate = request.DueDate,
                UserId = request.UserId!
            };

            await _taskRepository.CreateAsync(task);

            _logger.LogInformation("Task '{Title}' created with ID {TaskId}", task.Title, task.Id);

            return task;
        }

        public async Task<TaskItem> UpdateAsync(Guid id, UpdateTaskRequest request)
        {
            var task = await _taskRepository.GetByIdAsync(id)
                ?? throw new NotFoundException($"Task with ID {id} not found.");

            ValidateStatus(request.Status);

            task.Title = request.Title;
            task.Description = request.Description;
            task.Status = request.Status;
            task.DueDate = request.DueDate;

            await _taskRepository.UpdateAsync(task);

            _logger.LogInformation("Task '{Title}' (ID {TaskId}) updated", task.Title, task.Id);

            return task;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var task = await _taskRepository.GetByIdAsync(id)
                ?? throw new NotFoundException($"Task with ID {id} not found.");

            var result = await _taskRepository.DeleteAsync(id);

            _logger.LogInformation("Task '{Title}' (ID {TaskId}) deleted", task.Title, task.Id);

            return result;
        }

        private static void ValidateStatus(string status)
        {
            if (!TaskItemStatus.All.Contains(status))
            {
                throw new BusinessException($"Invalid status '{status}'. Valid values: {string.Join(", ", TaskItemStatus.All)}");
            }
        }
    }
}
