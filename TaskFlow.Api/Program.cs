using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenApiModels = Microsoft.OpenApi.Models;
using System.Text;
using TaskFlow.Api.Data;
using TaskFlow.Api.Models;
using System;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// 1. ПІДКЛЮЧЕННЯ ДО БД (MySQL)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var serverVersion = new MySqlServerVersion(new Version(8, 0, 31)); 

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, serverVersion));

// 2. Налаштування JWT
var jwtKey = builder.Configuration["Jwt:Key"] ?? "SuperSecretKeyForTaskFlowApp_1234567890_MakeItVeryLong_1234567890"; 
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 3. Налаштування Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiModels.OpenApiSecurityScheme
    {
        Description = "Введіть токен у форматі: Bearer {твій_токен}",
        Name = "Authorization",
        In = OpenApiModels.ParameterLocation.Header,
        Type = OpenApiModels.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiModels.OpenApiSecurityRequirement
    {
        {
            new OpenApiModels.OpenApiSecurityScheme
            {
                Reference = new OpenApiModels.OpenApiReference 
                { 
                    Type = OpenApiModels.ReferenceType.SecurityScheme, 
                    Id = "Bearer" 
                }
            },
            Array.Empty<string>()
        }
    });
});

// 4. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// --- БЕЗПЕЧНИЙ БЛОК БАЗИ ДАНИХ ТА АДМІНІСТРАТОРА ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<AppDbContext>();
        
        Console.WriteLine("--> Запуск міграцій...");
        db.Database.Migrate();
        Console.WriteLine("--> Міграції успішно перевірені/застосовані!");

        var adminEmail = "admin@taskflow.com";
        
        // Перевіряємо, чи існує адмін. Any() працює швидше і безпечніше
        if (!db.Users.Any(u => u.Email == adminEmail))
        {
            var adminUser = new User
            {
                Username = "Admin", 
                Email = adminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = "Admin",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Users.Add(adminUser);
            db.SaveChanges();
            Console.WriteLine("--> Адміністратора створено!");
        }
        else
        {
            Console.WriteLine("--> Адміністратор вже існує в базі.");
        }
    }
    catch (Exception ex)
    {
        // Якщо база даних видасть помилку, додаток все одно продовжить роботу!
        Console.WriteLine($"[CRITICAL] Помилка ініціалізації БД: {ex.Message}");
    }
}
// ----------------------------------------

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// 1. Статичні файли (фронтенд)
app.UseDefaultFiles(); 
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var path = ctx.File.Name;
        if (path.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            ctx.Context.Response.Headers["Pragma"] = "no-cache";
            ctx.Context.Response.Headers["Expires"] = "0";
        }
    }
});

app.UseCors("AllowAll");

// 2. Безпека
app.UseAuthentication();
app.UseAuthorization();

// 3. Маршрутизація API
app.MapControllers();

app.Run();