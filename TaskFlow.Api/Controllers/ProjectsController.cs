using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskFlow.Api.Data;
using TaskFlow.Api.DTOs;
using TaskFlow.Api.Models;

namespace TaskFlow.Api.Controllers
{
    [Authorize] // 🔐 МАГІЯ! Цей рядок закриває доступ усім, у кого немає токена
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProjectsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateProject(ProjectCreateDto request)
        {
            // 1. Дістаємо ID користувача прямо з його токена!
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
            
            long userId = long.Parse(userIdString);

            // 2. Створюємо проєкт і прив'язуємо до цього користувача
            var project = new Project
            {
                UserId = userId,
                Title = request.Title,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // 3. Зберігаємо в базу
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            return Ok(project);
        }
    }
}