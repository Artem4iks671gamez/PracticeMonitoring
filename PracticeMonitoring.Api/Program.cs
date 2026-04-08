using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PracticeMonitoring.Api.Data;
using PracticeMonitoring.Api.Entities;
using PracticeMonitoring.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Добавление контроллеров
builder.Services.AddControllers();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PracticeMonitoring.Api",
        Version = "v1",
        Description = "API системы мониторинга проведения производственных практик"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Введите JWT токен в формате: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Подключение БД
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Сервисы приложения
builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<JwtService>();

// CORS для frontend-проекта MVC
builder.Services.AddCors(options =>
{
    options.AddPolicy("WebPolicy", policy =>
    {
        policy
            .WithOrigins(
                "https://localhost:7290",
                "http://localhost:5290",
                "https://localhost:5001",
                "http://localhost:5000",
                "https://localhost:7128" 
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// JWT
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = jwtSettings["Key"]!;

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),

            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// --- seeding данных специальностей/групп из HelpingInfo/groups.csv ---
using (var scope = app.Services.CreateScope())
{
    var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // если уже есть данные — пропускаем
    if (!db.Specialties.Any())
    {
        var csvPath = Path.Combine(env.ContentRootPath, "HelpingInfo", "groups.csv");
        if (File.Exists(csvPath))
        {
            var lines = File.ReadAllLines(csvPath);
            if (lines.Length > 1)
            {
                // первая строка — заголовок
                for (int i = 1; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    // Простая разбивка: первые две колонки – код и название, остальные – курсы 1..n
                    var parts = line.Split(',');
                    if (parts.Length < 2) continue;

                    var code = parts[0].Trim();
                    var name = parts[1].Trim();

                    // создаём специальность, если ещё нет
                    var specialty = db.Specialties.FirstOrDefault(s => s.Code == code);
                    if (specialty == null)
                    {
                        specialty = new Specialty { Code = code, Name = name };
                        db.Specialties.Add(specialty);
                        db.SaveChanges();
                    }

                    // курсы начинаются с индекса 2 -> course = 1
                    for (int col = 2; col < parts.Length; col++)
                    {
                        var courseNumber = col - 1;
                        var groupsCell = parts[col].Trim();
                        if (string.IsNullOrWhiteSpace(groupsCell)) continue;

                        var groups = groupsCell.Split(';');
                        foreach (var g in groups)
                        {
                            var groupName = g.Trim();
                            if (string.IsNullOrWhiteSpace(groupName)) continue;

                            var exists = db.Groups.Any(gr => gr.Name == groupName && gr.SpecialtyId == specialty.Id);
                            if (!exists)
                            {
                                db.Groups.Add(new Group
                                {
                                    Name = groupName,
                                    Course = courseNumber,
                                    SpecialtyId = specialty.Id
                                });
                            }
                        }
                    }

                    db.SaveChanges();
                }
            }
        }
    }
}
// --- конец seeding ---

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS должен стоять до Authentication/Authorization
app.UseCors("WebPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();