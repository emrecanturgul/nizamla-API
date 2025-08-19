using AutoMapper;
using nizamla.Application.dtos;
using nizamla.Application.DTOs;
using nizamla.Core.Entities;
using nizamla.Core.Interfaces;

namespace nizamla.Application.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;
    private readonly IMapper _mapper;

    public TaskService(ITaskRepository taskRepository, IMapper mapper)
    {
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<IEnumerable<TaskItemDto>> GetAllTasksAsync()
    {
        try
        {
            var tasks = await _taskRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<TaskItemDto>>(tasks);
        }
        catch (Exception ex)
        {
            throw new Exception("Görevler getirilemedi", ex);
        }
    }

    public async Task<TaskItemDto?> GetTaskByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return null;

            var task = await _taskRepository.GetByIdAsync(id);
            return task != null ? _mapper.Map<TaskItemDto>(task) : null;
        }
        catch (Exception ex)
        {
            throw new Exception($"ID {id} olan görev getirilemedi", ex);
        }
    }

    public async Task<TaskItemDto> CreateTaskAsync(CreateTaskDto createTaskDto)
    {
        try
        {
            var taskEntity = _mapper.Map<TaskItem>(createTaskDto);
            var createdTask = await _taskRepository.CreateAsync(taskEntity);
            return _mapper.Map<TaskItemDto>(createdTask);
        }
        catch (Exception ex)
        {
            throw new Exception("Görev oluşturulamadı", ex);
        }
    }

    public async Task<TaskItemDto?> UpdateTaskAsync(int id, UpdateTaskDto updateTaskDto)
    {
        try
        {
            if (id <= 0)
                return null;

            var existingTask = await _taskRepository.GetByIdAsync(id);
            if (existingTask == null)
                return null;
            if (!string.IsNullOrWhiteSpace(updateTaskDto.Title))
                existingTask.Title = updateTaskDto.Title;

            if (updateTaskDto.Description != null)
                existingTask.Description = updateTaskDto.Description;

            if (updateTaskDto.DueDate.HasValue)
                existingTask.DueDate = updateTaskDto.DueDate;

            if (updateTaskDto.IsCompleted.HasValue)
                existingTask.IsCompleted = updateTaskDto.IsCompleted.Value;

            existingTask.UpdatedAt = DateTime.UtcNow;

            var updatedTask = await _taskRepository.UpdateAsync(existingTask);
            return updatedTask != null ? _mapper.Map<TaskItemDto>(updatedTask) : null;
        }
        catch (Exception ex)
        {
            throw new Exception($"ID {id} olan görev güncellenemedi", ex);
        }
    }

    public async Task<bool> DeleteTaskAsync(int id)
    {
        try
        {
            if (id <= 0)
                return false;

            return await _taskRepository.DeleteAsync(id);
        }
        catch (Exception ex)
        {
            throw new Exception($"ID {id} olan görev silinemedi", ex);
        }
    }

  
    public async Task<IEnumerable<TaskItemDto>> GetTasksByUserIdAsync(int userId)
    {
        var tasks = await _taskRepository.GetByUserIdAsync(userId);
        return _mapper.Map<IEnumerable<TaskItemDto>>(tasks);
    }

}