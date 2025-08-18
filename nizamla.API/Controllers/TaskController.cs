using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nizamla.Application.dtos;
using nizamla.Application.DTOs;
using nizamla.Application.Services;
namespace nizamla.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TaskController : ControllerBase
    {
        private readonly ITaskService _taskService;
        public TaskController(ITaskService taskService)
        {
            _taskService = taskService;
        }
        private int GetCurrentUserId()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(id)) throw new UnauthorizedAccessException();
            return int.Parse(id);
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskItemDto>>> GetMyTasks()
        {
            var userId = GetCurrentUserId();
            var tasks = await _taskService.GetTasksByUserIdAsync(userId);
            return Ok(tasks);
        }
        [HttpGet("{id:int}")]
        public async Task<ActionResult<TaskItemDto>> GetTaskById(int id)
        {
            var userId = GetCurrentUserId();
            var task = await _taskService.GetTaskByIdAsync(id);
            if (task == null || task.UserId != userId)
                return NotFound(); 
            return Ok(task);
        }
        [HttpPost]
        public async Task<ActionResult<TaskItemDto>> CreateTask([FromBody] CreateTaskDto createTaskDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var userId = GetCurrentUserId();
            createTaskDto.UserId = userId;
            var createdTask = await _taskService.CreateTaskAsync(createTaskDto);
            return CreatedAtAction(nameof(GetTaskById), new { id = createdTask.Id }, createdTask);
        }
        [HttpPut("{id:int}")]
        public async Task<ActionResult<TaskItemDto>> UpdateTask(int id, [FromBody] UpdateTaskDto updateTaskDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var userId = GetCurrentUserId();
            var existing = await _taskService.GetTaskByIdAsync(id);
            if (existing == null || existing.UserId != userId)
                return NotFound();
            var updatedTask = await _taskService.UpdateTaskAsync(id, updateTaskDto);
            return Ok(updatedTask);
        }
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var userId = GetCurrentUserId();
            var existing = await _taskService.GetTaskByIdAsync(id);
            if (existing == null || existing.UserId != userId)
                return NotFound();
            await _taskService.DeleteTaskAsync(id);
            return NoContent();
        }
    }
}
