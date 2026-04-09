using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PracticeMonitoring.Api.Data;
using PracticeMonitoring.Api.Dtos;
using PracticeMonitoring.Api.Entities;
using PracticeMonitoring.Api.Services;

namespace PracticeMonitoring.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly PasswordService _passwordService;
    private readonly JwtService _jwtService;

    public AuthController(
        AppDbContext context,
        PasswordService passwordService,
        JwtService jwtService)
    {
        _context = context;
        _passwordService = passwordService;
        _jwtService = jwtService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLower();

        var exists = await _context.Users.AnyAsync(x => x.Email == email);
        if (exists)
            return BadRequest(new { message = "Пользователь с таким email уже существует." });

        var role = await _context.Roles.FirstOrDefaultAsync(x => x.Name == request.Role);
        if (role is null)
            return BadRequest(new { message = "Указанная роль не существует." });

        Group? group = null;
        if (request.GroupId.HasValue)
        {
            group = await _context.Groups
                .Include(g => g.Specialty)
                .FirstOrDefaultAsync(g => g.Id == request.GroupId.Value);

            if (group is null)
                return BadRequest(new { message = "Выбранная группа не найдена." });
        }

        var user = new User
        {
            Surname = request.Surname.Trim(),
            FirstName = request.Name.Trim(),
            Patronymic = string.IsNullOrWhiteSpace(request.Patronymic) ? null : request.Patronymic.Trim(),
            FullName = string.IsNullOrWhiteSpace(request.Patronymic)
                ? $"{request.Surname.Trim()} {request.Name.Trim()}"
                : $"{request.Surname.Trim()} {request.Name.Trim()} {request.Patronymic.Trim()}",
            Email = email,
            RoleId = role.Id,
            Role = role,
            GroupId = group?.Id,
            Group = group
        };

        user.PasswordHash = _passwordService.HashPassword(user, request.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user);

        return Ok(new AuthResponse
        {
            Token = token,
            FullName = user.FullName,
            Role = user.Role.Name
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLower();

        var user = await _context.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Email == email);

        if (user is null)
            return Unauthorized(new { message = "Неверный email или пароль." });

        var validPassword = _passwordService.VerifyPassword(user, request.Password);
        if (!validPassword)
            return Unauthorized(new { message = "Неверный email или пароль." });

        var token = _jwtService.GenerateToken(user);

        return Ok(new AuthResponse
        {
            Token = token,
            FullName = user.FullName,
            Role = user.Role.Name
        });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserResponse>> Me()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim is null)
            return Unauthorized();

        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = await _context.Users
            .Include(x => x.Role)
            .Include(x => x.Group)
                .ThenInclude(g => g.Specialty)
            .FirstOrDefaultAsync(x => x.Id == userId);

        if (user is null)
            return NotFound();

        return Ok(new CurrentUserResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.Name,
            Surname = user.Surname,
            FirstName = user.FirstName,
            Patronymic = user.Patronymic,
            GroupId = user.GroupId,
            GroupName = user.Group?.Name,
            SpecialtyCode = user.Group?.Specialty?.Code,
            SpecialtyName = user.Group?.Specialty?.Name,
            AvatarUrl = null
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        return Ok(new { message = "Для JWT logout выполняется на клиенте: удалите токен." });
    }
}