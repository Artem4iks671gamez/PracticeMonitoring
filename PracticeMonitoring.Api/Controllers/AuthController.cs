using System.Security.Claims;
using System.Text.Json;
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
    private readonly AuditLogService _auditLogService;
    private readonly EmailVerificationService _emailVerificationService;
    private readonly AccountEmailService _accountEmailService;

    public AuthController(
        AppDbContext context,
        PasswordService passwordService,
        JwtService jwtService,
        AuditLogService auditLogService,
        EmailVerificationService emailVerificationService,
        AccountEmailService accountEmailService)
    {
        _context = context;
        _passwordService = passwordService;
        _jwtService = jwtService;
        _auditLogService = auditLogService;
        _emailVerificationService = emailVerificationService;
        _accountEmailService = accountEmailService;
    }

    [HttpPost("send-registration-code")]
    public async Task<IActionResult> SendRegistrationCode(SendRegistrationCodeRequest request, CancellationToken cancellationToken)
    {
        var email = EmailVerificationService.NormalizeEmail(request.Email);

        if (await _context.Users.AnyAsync(x => x.Email == email, cancellationToken))
            return BadRequest(new { message = "Пользователь с таким email уже существует." });

        var validationResult = await ValidateStudentRegistrationAsync(request, cancellationToken);
        if (validationResult is not null)
            return validationResult;

        var payloadJson = JsonSerializer.Serialize(request);
        var code = await _emailVerificationService.CreateCodeAsync(
            email,
            EmailVerificationService.RegistrationPurpose,
            payloadJson,
            cancellationToken);

        await _accountEmailService.SendRegistrationCodeAsync(email, code, cancellationToken);

        return Ok(new { message = "Код подтверждения отправлен на email." });
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(ConfirmRegistrationRequest request, CancellationToken cancellationToken)
    {
        var email = EmailVerificationService.NormalizeEmail(request.Email);

        if (await _context.Users.AnyAsync(x => x.Email == email, cancellationToken))
            return BadRequest(new { message = "Пользователь с таким email уже существует." });

        try
        {
            await _emailVerificationService.VerifyCodeAsync(
                email,
                EmailVerificationService.RegistrationPurpose,
                request.Code,
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        var validationResult = await ValidateStudentRegistrationAsync(request, cancellationToken);
        if (validationResult is not null)
            return validationResult;

        var role = await _context.Roles.FirstOrDefaultAsync(x => x.Name == "Student", cancellationToken);
        if (role is null)
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Базовая роль Student не настроена." });

        var group = await _context.Groups
            .Include(g => g.Specialty)
            .FirstOrDefaultAsync(g => g.Id == request.GroupId!.Value, cancellationToken);

        if (group is null)
            return BadRequest(new { message = "Выбранная группа не найдена." });

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
            GroupId = group.Id,
            Group = group,
            Theme = "light",
            IsActive = true,
            MustChangePassword = false
        };

        user.PasswordHash = _passwordService.HashPassword(user, request.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogRegisteredUserAsync(user);

        var token = _jwtService.GenerateToken(user);

        return Ok(new AuthResponse
        {
            Token = token,
            FullName = user.FullName,
            Role = user.Role!.Name,
            MustChangePassword = user.MustChangePassword
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var email = EmailVerificationService.NormalizeEmail(request.Email);

        var user = await _context.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Email == email);

        if (user is null)
            return Unauthorized(new { message = "Неверный email или пароль." });

        var validPassword = _passwordService.VerifyPassword(user, request.Password);
        if (!validPassword)
            return Unauthorized(new { message = "Неверный email или пароль." });

        if (!user.IsActive)
            return Unauthorized(new { message = "Аккаунт отключён. Обратитесь к администратору." });

        var token = _jwtService.GenerateToken(user);

        return Ok(new AuthResponse
        {
            Token = token,
            FullName = user.FullName,
            Role = user.Role!.Name,
            MustChangePassword = user.MustChangePassword
        });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var email = EmailVerificationService.NormalizeEmail(request.Email);

        var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (user is not null && user.IsActive)
        {
            var code = await _emailVerificationService.CreateCodeAsync(
                email,
                EmailVerificationService.PasswordResetPurpose,
                payloadJson: null,
                cancellationToken);

            await _accountEmailService.SendPasswordResetCodeAsync(email, code, cancellationToken);
        }

        return Ok(new { message = "Если аккаунт существует, код восстановления отправлен на email." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var email = EmailVerificationService.NormalizeEmail(request.Email);

        var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (user is null || !user.IsActive)
            return BadRequest(new { message = "Не удалось сменить пароль." });

        try
        {
            await _emailVerificationService.VerifyCodeAsync(
                email,
                EmailVerificationService.PasswordResetPurpose,
                request.Code,
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        user.PasswordHash = _passwordService.HashPassword(user, request.NewPassword);
        user.MustChangePassword = false;
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Пароль изменён. Теперь можно войти." });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
            return NotFound();

        if (!user.MustChangePassword)
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
                return BadRequest(new { message = "Введите текущий пароль." });

            if (!_passwordService.VerifyPassword(user, request.CurrentPassword))
                return BadRequest(new { message = "Текущий пароль указан неверно." });
        }

        user.PasswordHash = _passwordService.HashPassword(user, request.NewPassword);
        user.MustChangePassword = false;
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Пароль изменён." });
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
                .ThenInclude(g => g!.Specialty)
            .FirstOrDefaultAsync(x => x.Id == userId);

        if (user is null)
            return NotFound();

        return Ok(new CurrentUserResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role!.Name,
            Surname = user.Surname,
            FirstName = user.FirstName,
            Patronymic = user.Patronymic,
            GroupId = user.GroupId,
            GroupName = user.Group?.Name,
            SpecialtyCode = user.Group?.Specialty?.Code,
            SpecialtyName = user.Group?.Specialty?.Name,
            AvatarUrl = user.AvatarUrl,
            Theme = user.Theme,
            MustChangePassword = user.MustChangePassword
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        return Ok(new { message = "Для JWT logout выполняется на клиенте: удалите токен." });
    }

    private async Task<ActionResult?> ValidateStudentRegistrationAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var roleExists = await _context.Roles.AnyAsync(x => x.Name == "Student", cancellationToken);
        if (!roleExists)
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Базовая роль Student не настроена." });

        if (!request.GroupId.HasValue)
            return BadRequest(new { message = "Для регистрации необходимо выбрать группу." });

        var groupExists = await _context.Groups.AnyAsync(g => g.Id == request.GroupId.Value, cancellationToken);
        if (!groupExists)
            return BadRequest(new { message = "Выбранная группа не найдена." });

        return null;
    }
}
