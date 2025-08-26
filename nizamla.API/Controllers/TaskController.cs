using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nizamla.Application.dtos;
using nizamla.Application.DTOs;
using nizamla.Application.Exceptions;
using nizamla.Application.Services;
using System.Net;
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

        public TaskController(ITaskService taskService, ILogger<TaskController> logger, IMapper mapper)
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

        /// <summary>
        /// Kullanıcının kendi görevlerini listeler.
        /// </summary>
        /// <remarks>
        /// JWT token’daki kullanıcıya ait görevler döndürülür.
        /// </remarks>
        /// <response code="200">Görev listesi başarıyla getirildi.</response>
        /// <response code="401">JWT token eksik ya da geçersiz.</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<TaskItemDto>), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<ActionResult<IEnumerable<TaskItemDto>>> GetMyTasks()
        {
            var userId = GetCurrentUserId();
            return Ok(await _taskService.GetTasksByUserIdAsync(userId));
        }

        /// <summary>
        /// Belirli bir görevi getirir.
        /// </summary>
        /// <remarks>
        /// Sadece JWT token’daki kullanıcıya ait olan görevler sorgulanabilir.
        /// </remarks>
        /// <param name="id">Görev Id</param>
        /// <response code="200">Görev bulundu.</response>
        /// <response code="401">JWT token eksik ya da geçersiz.</response>
        /// <response code="403">Kullanıcının bu göreve erişim izni yok.</response>
        /// <response code="404">Görev bulunamadı.</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(TaskItemDto), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        public async Task<ActionResult<TaskItemDto>> GetTaskById(int id)
        {
            var userId = GetCurrentUserId();
            var task = await _taskService.GetTaskByIdAsync(id);

            if (task == null)
                throw new HttpException(HttpStatusCode.NotFound, "Görev bulunamadı.");

            if (task.UserId != userId)
                throw new HttpException(HttpStatusCode.Forbidden, "Bu göreve erişim izniniz yok.");

            return Ok(task);
        }

        /// <summary>
        /// Yeni görev oluşturur.
        /// </summary>
        /// <remarks>
        /// Kullanıcının JWT token kimliği üzerinden otomatik olarak UserId atanır.
        /// </remarks>
        /// <param name="request">Görev oluşturma isteği</param>
        /// <response code="201">Görev başarıyla oluşturuldu.</response>
        /// <response code="400">Doğrulama hatası.</response>
        [HttpPost]
        [ProducesResponseType(typeof(TaskItemDto), 201)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
        {
            var userId = GetCurrentUserId();
            var dto = _mapper.Map<CreateTaskDto>(request);
            dto.UserId = userId;

            var created = await _taskService.CreateTaskAsync(dto);

            return CreatedAtAction(nameof(GetTaskById), new { id = created.Id }, new
            {
                mesaj = "Görev başarıyla oluşturuldu.",
                gorev = created
            });
        }

        /// <summary>
        /// Mevcut görevi günceller.
        /// </summary>
        /// <param name="id">Görev Id</param>
        /// <param name="updateTaskDto">Güncellenecek alanlar</param>
        /// <response code="200">Görev başarıyla güncellendi.</response>
        /// <response code="400">Doğrulama hatası.</response>
        /// <response code="403">Bu görevi güncelleme izniniz yok.</response>
        /// <response code="404">Görev bulunamadı.</response>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(TaskItemDto), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        public async Task<ActionResult<TaskItemDto>> UpdateTask(int id, [FromBody] UpdateTaskDto updateTaskDto)
        {
            if (!ModelState.IsValid)
                throw new ValidationException("Geçersiz görev güncelleme isteği.");

            var userId = GetCurrentUserId();
            var existing = await _taskService.GetTaskByIdAsync(id);

            if (existing == null)
                throw new HttpException(HttpStatusCode.NotFound, "Güncellenecek görev bulunamadı.");

            if (existing.UserId != userId)
                throw new HttpException(HttpStatusCode.Forbidden, "Bu görevi güncelleme izniniz yok.");

            var updatedTask = await _taskService.UpdateTaskAsync(id, updateTaskDto);
            return Ok(updatedTask);
        }

        /// <summary>
        /// Bir görevi siler.
        /// </summary>
        /// <param name="id">Görev Id</param>
        /// <response code="204">Görev başarıyla silindi.</response>
        /// <response code="403">Bu görevi silme izniniz yok.</response>
        /// <response code="404">Görev bulunamadı.</response>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var userId = GetCurrentUserId();
            var existing = await _taskService.GetTaskByIdAsync(id);

            if (existing == null)
                throw new HttpException(HttpStatusCode.NotFound, "Silinecek görev bulunamadı.");

            if (existing.UserId != userId)
                throw new HttpException(HttpStatusCode.Forbidden, "Bu görevi silme izniniz yok.");

            await _taskService.DeleteTaskAsync(id);
            return NoContent();
        }

        /// <summary>
        /// Sayfalama ile görevleri getirir.
        /// </summary>
        /// <param name="page">Sayfa numarası (1’den başlar)</param>
        /// <param name="pageSize">Sayfa boyutu</param>
        /// <param name="isCompleted">Tamamlanma durumu (opsiyonel)</param>
        /// <param name="sortBy">Sıralama kriteri: dueDate, createdAt</param>
        /// <response code="200">Sayfalı görev listesi.</response>
        /// <response code="400">Sayfa numarası veya boyutu hatalı.</response>
        [HttpGet("paged")]
        [ProducesResponseType(typeof(PagedResult<TaskItemDto>), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> GetPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] bool? isCompleted = null,
            [FromQuery] string? sortBy = null)
        {
            if (page <= 0 || pageSize <= 0)
                throw new ValidationException("Sayfa ve sayfa boyutu sıfırdan büyük olmalıdır.");

            var userId = GetCurrentUserId();
            var result = await _taskService.GetPagedTasksAsync(userId, page, pageSize, isCompleted, sortBy);
            return Ok(result);
        }
    }
}
