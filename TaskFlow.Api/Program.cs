using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenApiModels = Microsoft.OpenApi.Models;
using System.Text;
using TaskFlow.Api.Data;
using TaskFlow.Api.Models; // Додав для доступу до моделі User

var builder = WebApplication.CreateBuilder(args);

// 1. Підключення до БД (PostgreSQL)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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

// --- БЛОК БАЗИ ДАНИХ ТА АДМІНІСТРАТОРА ---
// --- БЛОК БАЗИ ДАНИХ ТА АДМІНІСТРАТОРА ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    var adminEmail = "admin@taskflow.com";
    var adminUser = db.Users.FirstOrDefault(u => u.Email == adminEmail);

    if (adminUser == null)
    {
        // Якщо адміна немає — створюємо з хешованим паролем
        adminUser = new User
        {
            Username = "Admin", 
            Email = adminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"), // ШИФРУЄМО ТУТ
            Role = "Admin",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Users.Add(adminUser);
        db.SaveChanges();
        Console.WriteLine("--> Admin account created with BCrypt hash!");
    }
    else if (adminUser.PasswordHash == "Admin123!")
    {
        // Якщо адмін є, але пароль там звичайним текстом (після минулої спроби)
        // оновлюємо його на хеш
        adminUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!");
        db.SaveChanges();
        Console.WriteLine("--> Admin password updated to BCrypt hash!");
    }
}
// ----------------------------------------

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();