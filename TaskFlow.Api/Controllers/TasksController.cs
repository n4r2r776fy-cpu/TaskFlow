using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskFlow.Api.Data;
using TaskFlow.Api.Models;

namespace TaskFlow.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
   // [Authorize] // Всі методи тут вимагають токен!
    public class TasksController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TasksController(AppDbContext context)
        {
            _context = context;
        }

        // 1. СТВОРЕННЯ ЗАВДАННЯ
        [HttpPost]
        public async Task<IActionResult> CreateTask([FromBody] TaskCreateDto dto)
        {
            // Дістаємо ID користувача з токена
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !long.TryParse(userIdString, out long userId))
            {
                return Unauthorized("Недійсний токен користувача.");
            }

            // Якщо передали ProjectId, перевіряємо, чи існує такий проєкт і чи належить він цьому юзеру
            if (dto.ProjectId.HasValue)
            {
                var project = await _context.Projects
                    .FirstOrDefaultAsync(p => p.Id == dto.ProjectId.Value && p.UserId == userId);
                
                if (project == null)
                    return BadRequest("Проєкт не знайдено, або у вас немає до нього доступу.");
            }

            var newTask = new TaskItem
            {
                Title = dto.Title,
                Description = dto.Description,
                ProjectId = dto.ProjectId,
                Priority = dto.Priority,
                UserId = userId,
                Status = "todo",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                DueDate = dto.DueDate // ДОДАНО ДЕДЛАЙН
            };

            _context.Tasks.Add(newTask);
            await _context.SaveChangesAsync();

            return Ok(newTask);
        }

        // 2. ОТРИМАННЯ ВСІХ ЗАВДАНЬ КОРИСТУВАЧА
        [HttpGet]
        public async Task<IActionResult> GetMyTasks()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !long.TryParse(userIdString, out long userId))
            {
                return Unauthorized();
            }

            var tasks = await _context.Tasks
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return Ok(tasks);
        }

        // 3. ОНОВЛЕННЯ ЗАВДАННЯ (Зміна статусу або тексту)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(long id, [FromBody] TaskCreateDto dto, [FromQuery] string? newStatus)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdString, out long userId)) return Unauthorized();

            // Шукаємо завдання, яке належить саме цьому юзеру
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            if (task == null) return NotFound("Завдання не знайдено.");

            // Оновлюємо дані
            task.Title = dto.Title;
            task.Description = dto.Description;
            task.Priority = dto.Priority;
            task.ProjectId = dto.ProjectId;
            task.UpdatedAt = DateTime.UtcNow;
            task.DueDate = dto.DueDate; // ДОДАНО ДЕДЛАЙН

            // Якщо передали новий статус — оновлюємо і його
            if (!string.IsNullOrEmpty(newStatus))
            {
                task.Status = newStatus;
                // Якщо статус "done", записуємо час завершення
                if (newStatus == "done" && task.CompletedAt == null)
                    task.CompletedAt = DateTime.UtcNow;
                else if (newStatus != "done")
                    task.CompletedAt = null; // Якщо повернули в роботу
            }

            await _context.SaveChangesAsync();
            return Ok(task);
        }

        // 4. ВИДАЛЕННЯ ЗАВДАННЯ
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(long id)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdString, out long userId)) return Unauthorized();

            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            if (task == null) return NotFound("Завдання не знайдено.");

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Завдання успішно видалено." });
        }
    }
}