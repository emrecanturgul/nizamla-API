using Microsoft.EntityFrameworkCore;
using nizamla.Core.Entities;
using nizamla.Core.Interfaces;
using nizamla.Infrastructure.Data;

namespace nizamla.Infrastructure.Repositories
{
    public class TaskRepository : ITaskRepository
    {
        private readonly AppDbContext _context;

        public TaskRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<TaskItem> CreateAsync(TaskItem taskItem)
        {
            taskItem.CreatedAt = DateTime.UtcNow;
            taskItem.UpdatedAt = DateTime.UtcNow;
            _context.TaskItems.Add(taskItem);
            await _context.SaveChangesAsync();
            return taskItem;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var taskItem = await _context.TaskItems.FindAsync(id);
            if (taskItem == null)
                return false;

            _context.TaskItems.Remove(taskItem);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.TaskItems.AnyAsync(t => t.Id == id);
        }

        public async Task<IEnumerable<TaskItem>> GetAllAsync()
        {
            return await _context.TaskItems
                .OrderBy(t => t.IsCompleted)
                .ThenByDescending(t => t.CreatedAt)
                .ToListAsync();
        }
        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.Email == email);
        }
        public async Task<TaskItem?> GetByIdAsync(int id) 
        {
            return await _context.TaskItems.FindAsync(id); 
        }

        public async Task<IEnumerable<TaskItem>> GetByUserIdAsync(int userId)
        {
            return await _context.TaskItems.Where(t=> t.UserId == userId).OrderBy(t=>t.IsCompleted).ThenByDescending(t=>t.CreatedAt).ToListAsync();
        }

        public async Task<(IEnumerable<TaskItem> Items, int TotalCount)> GetPagedAsync(
    int page, int pageSize, bool? isCompleted, string? sortBy)
        {
            var query = _context.TaskItems.AsQueryable();

            
            if (isCompleted.HasValue)
                query = query.Where(t => t.IsCompleted == isCompleted.Value);

           
            query = sortBy switch
            {
                "dueDate" => query.OrderBy(t => t.DueDate),
                "createdAt" => query.OrderByDescending(t => t.CreatedAt),
                _ => query.OrderBy(t => t.Id)
            };

            
            var totalCount = await query.CountAsync();

            
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<TaskItem?> UpdateAsync(TaskItem taskItem)
        {
            var existingTask = await _context.TaskItems.FindAsync(taskItem.Id);
            if (existingTask == null)
                return null;
            existingTask.Title = taskItem.Title;
            existingTask.Description = taskItem.Description;
            existingTask.DueDate = taskItem.DueDate;
            existingTask.IsCompleted = taskItem.IsCompleted;
            existingTask.UpdatedAt = DateTime.UtcNow;

            _context.Entry(existingTask).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return existingTask;
        }
    }
}