using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using nizamla.Infrastructure.Data;

namespace nizamla.API.Controllers   
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                Status = "Hello World from Nizamla!",
                Timestamp = DateTime.UtcNow
            });
        }

        [HttpGet("info")]
        public IActionResult GetInfo()
        {
            return Ok(new
            {
                ProjectName = "Nizamla",
                Description = "task manager",
                Version = "1.0.0",
                
            });
        }

        [HttpGet("db-test")]
        public async Task<IActionResult> DbTest([FromServices] AppDbContext context)
        {
            try
            {
                var canConnect = await context.Database.CanConnectAsync();
                var userCount = await context.Users.CountAsync();

                return Ok(new
                {
                    message = "nizamla api",
                    canConnect,
                    userCount,
                    databaseName = "NizamlaDB",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    error = "Database connection failed",
                    details = ex.Message
                });
            }
        }
    }
}