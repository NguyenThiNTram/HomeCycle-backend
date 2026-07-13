using Google;
using HomeCycle.Application.Interfaces.Services.Externals;
using HomeCycle.Infrastructure.DbContexts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HomeCycle.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MediaController : ControllerBase
    {
        private readonly IFileStorageService _storageService;
        private readonly HomeCycleDbContext _db;

        public MediaController(IFileStorageService storageService, HomeCycleDbContext dbContext)
        {
            _storageService = storageService;
            _db = dbContext;
        }

        // API NHẬN UPLOAD (Web và Mobile dùng chung)
        [HttpPost("upload-avatar/{userId}")]
        public async Task<IActionResult> UploadAvatar(int userId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File không hợp lệ.");

            var user = await _db.Users.FindAsync(userId);
            if (user == null) return NotFound("User không tồn tại.");

            using (var stream = file.OpenReadStream())
            {
                // đẩy lên Firebase và trả về Tên file duy nhất
                string uniqueFileName = await _storageService.UploadFileAsync(stream, file.FileName);

                // Lưu TÊN FILE vào db
                user.AvatarUrl = uniqueFileName;
                await _db.SaveChangesAsync();

                string fullUrl = _storageService.GetFileUrl(uniqueFileName);

                return Ok(new { Message = "Thành công", FileName = uniqueFileName, Url = fullUrl });
            }
        }

        // API LẤY THÔNG TIN ĐỂ HIỂN THỊ (Trả về Link URL đầy đủ trực tiếp)
        [HttpGet("user-profile/{userId}")]
        public async Task<IActionResult> GetProfile(int userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return NotFound();

            string finalImgUrl = _storageService.GetFileUrl(user.AvatarUrl);

            return Ok(new
            {
                Id = user.UserId,
                Username = user.Username,
                Avatar = finalImgUrl
            });
        }
    }
}
