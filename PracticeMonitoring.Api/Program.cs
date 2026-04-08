using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PracticeMonitoring.Api.Data;
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
                "http://localhost:5000"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
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