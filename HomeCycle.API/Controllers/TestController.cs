using HomeCycle.Infrastructure.DbContexts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly HomeCycleDbContext _context;

        public TestController(HomeCycleDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Kiểm tra API hoạt động
        /// </summary>
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                success = true,
                message = "HomeCycle API is running.",
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Kiểm tra kết nối Database
        /// </summary>
        [HttpGet("database")]
        public async Task<IActionResult> TestDatabase()
        {
            try
            {
                await _context.Database.OpenConnectionAsync();

                var connection = _context.Database.GetDbConnection();

                return Ok(new
                {
                    success = true,
                    database = connection.Database,
                    server = connection.DataSource
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    error = ex.ToString()
                });
            }
        }
    }
}
