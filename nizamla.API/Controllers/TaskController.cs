using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using nizamla.Application.DTOs;
using nizamla.Application.Services;
using nizamla.Core.Entities;

namespace nizamla.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TaskController : ControllerBase
    {
        private readonly ITaskService _taskService;

        public TaskController(ITaskService taskService)
        {
            _taskService = taskService;
        }
        [HttpGet]
        public async Task<IActionResult> GetAllTasks()
        {
            var tasks = await _taskService.GetAllTasksAsync();
            return Ok(tasks);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTaskById(int id)
        {
            var task = await _taskService.GetTaskByIdAsync(id);
            if (task == null)
                return NotFound($"ID {id} olan görev bulunamadı");

            return Ok(task);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTask([FromBody] TaskItem taskItem)
        {
            if (taskItem == null)
                return BadRequest("Görev bilgisi boş olamaz");

            var createdTask = await _taskService.CreateTaskAsync(taskItem);
            return CreatedAtAction(nameof(GetTaskById), new { id = createdTask.Id }, createdTask);
        }
        
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var result = await _taskService.DeleteTaskAsync(id);
            if (!result)
                return NotFound($"ID {id} olan görev silinemedi");

            return NoContent();
        }
        

    }
}
