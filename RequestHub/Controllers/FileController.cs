using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RequestHub.Data;
using RequestHub.Models;
using System.Security.Claims;

namespace RequestHub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FileController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public FileController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private int GetCurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost("{requestId}/upload")]
        public async Task<IActionResult> Upload(int requestId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file provided.");

            // Check request exists
            var request = await _context.AccessRequests.FindAsync(requestId);
            if (request == null) return NotFound("Request not found.");

            // Save file to uploads folder
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsFolder);

            var storedName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, storedName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            // Save file info to DB
            var requestFile = new RequestFile
            {
                RequestId = requestId,
                FileName = file.FileName,
                StoredName = storedName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                UploadedBy = GetCurrentUserId(),
                UploadedAt = DateTime.UtcNow
            };

            _context.RequestFiles.Add(requestFile);
            await _context.SaveChangesAsync();

            return Ok(new { requestFile.Id, requestFile.FileName, requestFile.FileSize });
        }

        [HttpGet("{requestId}/files")]
        public async Task<IActionResult> GetFiles(int requestId)
        {
            var files = await _context.RequestFiles
                .Where(f => f.RequestId == requestId)
                .Select(f => new { f.Id, f.FileName, f.FileSize, f.UploadedAt, f.ContentType })
                .ToListAsync();
            return Ok(files);
        }

        [HttpGet("download/{fileId}")]
        public async Task<IActionResult> Download(int fileId)
        {
            var file = await _context.RequestFiles.FindAsync(fileId);
            if (file == null) return NotFound();

            var filePath = Path.Combine(_env.WebRootPath, "uploads", file.StoredName);
            if (!System.IO.File.Exists(filePath)) return NotFound("File not found on disk.");

            var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(bytes, file.ContentType, file.FileName);
        }

        [HttpDelete("{fileId}")]
        public async Task<IActionResult> Delete(int fileId)
        {
            var file = await _context.RequestFiles.FindAsync(fileId);
            if (file == null) return NotFound();

            // Delete from disk
            var filePath = Path.Combine(_env.WebRootPath, "uploads", file.StoredName);
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);

            _context.RequestFiles.Remove(file);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}