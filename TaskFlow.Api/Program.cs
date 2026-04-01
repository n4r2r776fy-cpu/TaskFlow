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

// --- 1. ПІДКЛЮЧЕННЯ ДО БД (PostgreSQL) ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options => 
    options.UseNpgsql(connectionString));

// --- 2. НАЛАШТУВАННЯ JWT ---
var jwtKey = builder.Configuration["Jwt:Key"] ?? "SuperSecretKeyForTaskFlowApp_1234567890_MakeItVeryLong_1234567890"; 
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero // Прибирає затримку валідації часу токена
        };
    });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// --- 3. НАЛАШТУВАННЯ SWAGGER ---
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

// --- 4. CORS ПРАВИЛА ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// --- 5. ІНІЦІАЛІЗАЦІЯ БАЗИ ДАНИХ ТА АДМІНІСТРАТОРА ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<AppDbContext>(); 
        
        Console.WriteLine("--> Запуск міграцій PostgreSQL...");
        db.Database.Migrate();
        Console.WriteLine("--> Міграції успішно застосовані!");

        var adminEmail = "admin@taskflow.com";
        
        if (!db.Users.Any(u => u.Email == adminEmail))
        {
            var adminUser = new User
            {
                Username = "Admin", 
                Email = adminEmail,
                // Використовуємо BCrypt для хешування
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
            Console.WriteLine("--> Адміністратор вже існує.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[CRITICAL] Помилка БД: {ex.Message}");
    }
}

// --- 6. MIDDLEWARE (ПОРЯДОК ВАЖЛИВИЙ) ---

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles(); 
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Вимикаємо кешування для HTML, щоб бачити зміни в скриптах відразу
        if (ctx.File.Name.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            ctx.Context.Response.Headers["Pragma"] = "no-cache";
            ctx.Context.Response.Headers["Expires"] = "0";
        }
    }
});

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();