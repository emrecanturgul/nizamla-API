using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nizamla.Application.dtos;
using nizamla.Application.DTOs;
using nizamla.Application.Services;
using System.Security.Claims;

namespace nizamla.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TaskController : ControllerBase
    {
        private readonly ITaskService _taskService;
        private readonly ILogger<TaskController> _logger;
        private readonly IMapper _mapper;

        public TaskController(ITaskService taskService, ILogger<TaskController> logger,IMapper mapper)
        {
            _taskService = taskService;
            _logger = logger;
            _mapper = mapper;
        }

        private int GetCurrentUserId()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogWarning("JWT token içinde kullanıcı kimliği bulunamadı.");
                throw new UnauthorizedAccessException();
            }

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
            _logger.LogInformation("Kullanıcı {UserId}, TaskId {TaskId} görevini sorguluyor.", userId, id);

            var task = await _taskService.GetTaskByIdAsync(id);

            if (task == null || task.UserId != userId)
            {
                _logger.LogWarning("Kullanıcı {UserId}, kendisine ait olmayan veya bulunamayan göreve erişmeye çalıştı. TaskId={TaskId}", userId, id);
                return NotFound();
            }

            _logger.LogInformation("Kullanıcı {UserId}, TaskId {TaskId} görevini görüntüledi.", userId, id);
            return Ok(task);
        }

        [HttpPost]
        public async Task<ActionResult<TaskItemDto>> CreateTask([FromBody] CreateTaskRequest request)
        {
            var userId = GetCurrentUserId();
            var dto = _mapper.Map<CreateTaskDto>(request);
            dto.UserId = userId; // JWT’den
            var created = await _taskService.CreateTaskAsync(dto);
            return CreatedAtAction(nameof(GetTaskById), new { id = created.Id }, created);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<TaskItemDto>> UpdateTask(int id, [FromBody] UpdateTaskDto updateTaskDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Geçersiz güncelleme isteği: {@ModelState}", ModelState);
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            _logger.LogInformation("Kullanıcı {UserId}, TaskId {TaskId} görevini güncellemeye çalışıyor.", userId, id);

            var existing = await _taskService.GetTaskByIdAsync(id);
            if (existing == null || existing.UserId != userId)
            {
                _logger.LogWarning("Kullanıcı {UserId}, kendisine ait olmayan veya bulunamayan bir görevi güncellemeye çalıştı. TaskId={TaskId}", userId, id);
                return NotFound();
            }

            var updatedTask = await _taskService.UpdateTaskAsync(id, updateTaskDto);
            _logger.LogInformation("Kullanıcı {UserId}, TaskId {TaskId} görevini güncelledi.", userId, id);

            return Ok(updatedTask);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("Kullanıcı {UserId}, TaskId {TaskId} görevini silmeye çalışıyor.", userId, id);

            var existing = await _taskService.GetTaskByIdAsync(id);
            if (existing == null || existing.UserId != userId)
            {
                _logger.LogWarning("Kullanıcı {UserId}, kendisine ait olmayan veya bulunamayan görevi silmeye çalıştı. TaskId={TaskId}", userId, id);
                return NotFound();
            }

            await _taskService.DeleteTaskAsync(id);
            _logger.LogInformation("Kullanıcı {UserId}, TaskId {TaskId} görevini sildi.", userId, id);

            return NoContent();
        }

      
    }
}
