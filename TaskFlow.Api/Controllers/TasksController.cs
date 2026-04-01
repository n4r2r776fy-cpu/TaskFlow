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
    [Authorize] // 1. ПРАВКА: Обов'язково вмикаємо Authorize для захисту даних (JWT за ТЗ)
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
            var userId = GetUserId();
            if (userId == -1) return Unauthorized();

            if (dto.ProjectId.HasValue)
            {
                var project = await _context.Projects
                    .FirstOrDefaultAsync(p => p.Id == dto.ProjectId.Value && p.UserId == userId);
                
                if (project == null)
                    return BadRequest("Проєкт не знайдено.");
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
                DueDate = dto.DueDate
            };

            _context.Tasks.Add(newTask);
            await _context.SaveChangesAsync();

            return Ok(newTask);
        }

        // 2. ОТРИМАННЯ ВСІХ ЗАВДАННЯ (Дашборд)
        [HttpGet]
        public async Task<IActionResult> GetMyTasks()
        {
            var userId = GetUserId();
            if (userId == -1) return Unauthorized();

            // 2. ПРАВКА: Додаємо Include(t => t.TimeLogs) для автоматичного підрахунку часу
            var tasks = await _context.Tasks
                .Include(t => t.TimeLogs) 
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new {
                    t.Id,
                    t.Title,
                    t.Status,
                    t.Priority,
                    t.DueDate,
                    t.CompletedAt,
                    // Автоматичний підрахунок за ТЗ:
                    TotalTimeSpent = t.TimeLogs.Sum(l => l.TimeSpent) 
                })
                .ToListAsync();

            return Ok(tasks);
        }

        // 3. ОТРИМАННЯ ДЕТАЛЕЙ ЗАВДАННЯ (Сторінка деталей за ТЗ)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTaskDetails(long id)
        {
            var userId = GetUserId();
            
            // Завантажуємо завдання разом з усіма логами часу
            var task = await _context.Tasks
                .Include(t => t.TimeLogs)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (task == null) return NotFound();

            return Ok(new {
                task.Id,
                task.Title,
                task.Description,
                task.Status,
                task.Priority,
                task.DueDate,
                task.CompletedAt,
                TotalTimeSpent = task.TimeLogs.Sum(l => l.TimeSpent),
                Logs = task.TimeLogs.OrderByDescending(l => l.LoggedAt) // Список усіх записів прогресу
            });
        }

        // 4. ОНОВЛЕННЯ ЗАВДАННЯ (Зміна статусу)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(long id, [FromBody] TaskCreateDto dto, [FromQuery] string? newStatus)
        {
            var userId = GetUserId();
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            if (task == null) return NotFound();

            task.Title = dto.Title;
            task.Description = dto.Description;
            task.Priority = dto.Priority;
            task.ProjectId = dto.ProjectId;
            task.DueDate = dto.DueDate;
            task.UpdatedAt = DateTime.UtcNow;

            // 3. ПРАВКА: Чітка логіка статусів за ТЗ
            if (!string.IsNullOrEmpty(newStatus))
            {
                task.Status = newStatus.ToLower();
                
                if (task.Status == "done")
                {
                    // Автоматична фіксація за ТЗ:
                    task.CompletedAt = DateTime.UtcNow; 
                }
                else
                {
                    task.CompletedAt = null;
                }
            }

            await _context.SaveChangesAsync();
            return Ok(task);
        }

        // 5. ДОПОМІЖНИЙ МЕТОД (щоб не дублювати код)
        private long GetUserId()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(userIdString, out long userId) ? userId : -1;
        }
    }
}