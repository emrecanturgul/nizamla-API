using nizamla.Core.Entities;
using nizamla.Core.Interfaces;

namespace nizamla.Application.Services
{
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _taskRepository;

        public TaskService(ITaskRepository taskRepository)
        {
            _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        }

        public async Task<IEnumerable<TaskItem>> GetAllTasksAsync()
        {
            try
            {
                return await _taskRepository.GetAllAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Görevler getirilemedi", ex);
            }
        }

        public async Task<TaskItem?> GetTaskByIdAsync(int id) // ✅ Nullable return
        {
            try
            {
                if (id <= 0)
                    return null;

                return await _taskRepository.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                throw new Exception($"ID {id} olan görev getirilemedi", ex);
            }
        }

        public async Task<TaskItem> CreateTaskAsync(TaskItem taskItem)
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(taskItem.Title))
                    throw new ArgumentException("Görev başlığı boş olamaz");

                // Business logic
                taskItem.IsCompleted = false;
                taskItem.CreatedAt = DateTime.UtcNow;
                taskItem.UpdatedAt = DateTime.UtcNow;

                return await _taskRepository.CreateAsync(taskItem);
            }
            catch (Exception ex)
            {
                throw new Exception("Görev oluşturulamadı", ex);
            }
        }

        public async Task<TaskItem?> UpdateTaskAsync(TaskItem taskItem) // ✅ Nullable return
        {
            try
            {
                // Validation
                if (taskItem.Id <= 0)
                    return null;

                if (string.IsNullOrWhiteSpace(taskItem.Title))
                    throw new ArgumentException("Görev başlığı boş olamaz");

                return await _taskRepository.UpdateAsync(taskItem);
            }
            catch (Exception ex)
            {
                throw new Exception($"ID {taskItem.Id} olan görev güncellenemedi", ex);
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
    }
}