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
        Task<IEnumerable<TaskItem>> GetAllTasksAsync();
        Task<TaskItem?> GetTaskByIdAsync(int id);
        Task<TaskItem> CreateTaskAsync(TaskItem taskItem); 
        Task<TaskItem?> UpdateTaskAsync(TaskItem taskItem); 
        Task<bool> DeleteTaskAsync(int id);
    }
}