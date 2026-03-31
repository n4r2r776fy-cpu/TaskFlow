using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenApiModels = Microsoft.OpenApi.Models;
using System.Text;
using TaskFlow.Api.Data;
using TaskFlow.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. ПІДКЛЮЧЕННЯ ДО БД (Змінено на MySQL)
// Замість старого блоку з AutoDetect
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var serverVersion = new MySqlServerVersion(new Version(8, 0, 31)); // Версія 8.0 як на хостингу

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

// --- БЛОК БАЗИ ДАНИХ ТА АДМІНІСТРАТОРА ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    // Автоматичне застосування міграцій при старті
    db.Database.Migrate();

    var adminEmail = "admin@taskflow.com";
    var adminUser = db.Users.FirstOrDefault(u => u.Email == adminEmail);

    if (adminUser == null)
    {
        // Якщо адміна взагалі немає - створюємо
        adminUser = new User
        {
            Username = "Admin", 
            Email = adminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Role = "Admin",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Users.Add(adminUser);
        Console.WriteLine("--> Admin account created with BCrypt hash!");
    }
    else
    {
        // Якщо адмін є, але щось зламалося - ПРИМУСОВО ВІДНОВЛЮЄМО
        adminUser.Role = "Admin";
        adminUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!");
        adminUser.UpdatedAt = DateTime.UtcNow;
        db.Users.Update(adminUser);
        Console.WriteLine("--> Admin account restored/updated to default password and role!");
    }
    
    db.SaveChanges(); // Зберігаємо зміни в будь-якому випадку
}
// ----------------------------------------

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// 1. Статичні файли (фронтенд) мають бути на початку
app.UseDefaultFiles(); // Дозволяє відкривати index.html за замовчуванням
app.UseStaticFiles();  // Дозволяє завантажувати CSS, JS, картинки

app.UseCors("AllowAll");

// 2. Потім безпека
app.UseAuthentication();
app.UseAuthorization();

// 3. І в самому кінці — маршрутизація API
app.MapControllers();

app.Run();