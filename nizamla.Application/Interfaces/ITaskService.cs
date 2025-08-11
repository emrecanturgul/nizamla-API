using nizamla.Application.dtos;
using nizamla.Application.DTOs;
using nizamla.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nizamla.Application.Services
{
    public interface ITaskService
    {
        Task<IEnumerable<TaskItemDto>> GetAllTasksAsync();
        Task<TaskItemDto?> GetTaskByIdAsync(int id);
        Task<TaskItemDto> CreateTaskAsync(CreateTaskDto createTaskDto);
        Task<TaskItemDto?> UpdateTaskAsync(int id, UpdateTaskDto updateTaskDto);
        Task<bool> DeleteTaskAsync(int id);
    }
}