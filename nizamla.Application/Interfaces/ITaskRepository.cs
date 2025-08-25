using nizamla.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nizamla.Core.Interfaces
{
    public interface ITaskRepository
    {
        Task<IEnumerable<TaskItem>> GetAllAsync();
        Task<TaskItem?> GetByIdAsync(int id); 
        Task<TaskItem> CreateAsync(TaskItem taskItem);
        Task<TaskItem?> UpdateAsync(TaskItem taskItem);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<IEnumerable<TaskItem>> GetByUserIdAsync(int userId);
        Task<(IEnumerable<TaskItem> Items, int TotalCount)> GetPagedAsync(
    int userId, int page, int pageSize, bool? isCompleted, string? sortBy);



    }
}