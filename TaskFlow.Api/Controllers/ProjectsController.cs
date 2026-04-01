using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore; // 👈 ВАЖЛИВО! Додано для роботи з базою
using TaskFlow.Api.Data;
using TaskFlow.Api.DTOs;
using TaskFlow.Api.Models;

namespace TaskFlow.Api.Controllers
{
   // [Authorize] // 🔐 МАГІЯ! Цей рядок закриває доступ усім, у кого немає токена
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProjectsController(AppDbContext context)
        {
            _context = context;
        }

        // --- 1. СТВОРЕННЯ ПРОЄКТУ (POST) ---
        [HttpPost]
        public async Task<IActionResult> CreateProject(ProjectCreateDto request)
        {
            // Дістаємо ID користувача прямо з його токена!
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
            
            long userId = long.Parse(userIdString);

            // Створюємо проєкт і прив'язуємо до цього користувача
            var project = new Project
            {
                UserId = userId,
                Title = request.Title,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Зберігаємо в базу
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            return Ok(project);
        }

        // --- 2. ОТРИМАННЯ СПИСКУ ПРОЄКТІВ (GET) - НОВЕ! ---
        [HttpGet]
        public async Task<IActionResult> GetProjects()
        {
            // 1. Дістаємо ID поточного користувача з токена
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
            
            long userId = long.Parse(userIdString);

            // 2. Шукаємо в базі тільки ті проєкти, де UserId збігається з нашим
            var projects = await _context.Projects
                .Where(p => p.UserId == userId)
                .ToListAsync();

            // 3. Віддаємо масив проєктів назад на фронтенд
            return Ok(projects);
        }
        // --- 3. ВИДАЛЕННЯ ПРОЄКТУ (DELETE) ---
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject(long id)
        {
            // Дістаємо ID користувача
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
            long userId = long.Parse(userIdString);

            // Шукаємо проєкт. Перевіряємо, чи він існує і чи належить цьому юзеру
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (project == null) 
                return NotFound(new { message = "Проєкт не знайдено або немає доступу." });

            // Видаляємо з бази
            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Проєкт успішно видалено!" });
        }
    }
}
