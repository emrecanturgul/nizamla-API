using nizamla.Application.dtos;
using nizamla.Application.DTOs;
using nizamla.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace nizamla.Application.Services
{
    public interface ITaskService
    {
        Task<IEnumerable<TaskItemDto>> GetAllTasksAsync();
        Task<TaskItemDto?> GetTaskByIdAsync(int id);
        Task<TaskItemDto> CreateTaskAsync(CreateTaskDto createTaskDto , int userId);
        Task<TaskItemDto?> UpdateTaskAsync(int id, UpdateTaskDto updateTaskDto);
        Task<bool> DeleteTaskAsync(int id);
        Task<IEnumerable<TaskItemDto>> GetTasksByUserIdAsync(int userId);
        Task<PagedResult<TaskItemDto>> GetPagedTasksAsync(int userId, int page, int pageSize, bool? isCompleted, string? sortBy);
    }
}
