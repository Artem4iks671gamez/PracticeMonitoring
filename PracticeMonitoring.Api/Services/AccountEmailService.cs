using System.Net;
using PracticeMonitoring.Api.Entities;

namespace PracticeMonitoring.Api.Services;

public class AccountEmailService
{
    private readonly EmailService _emailService;

    public AccountEmailService(EmailService emailService)
    {
        _emailService = emailService;
    }

    public Task SendRegistrationCodeAsync(string email, string code, CancellationToken cancellationToken = default)
        => _emailService.SendAsync(
            email,
            "Код регистрации Practice Monitoring",
            $"""
            <p>Ваш код подтверждения регистрации:</p>
            <h2>{WebUtility.HtmlEncode(code)}</h2>
            <p>Код действует 15 минут. Если вы не регистрировались, просто игнорируйте это письмо.</p>
            """,
            cancellationToken);

    public Task SendPasswordResetCodeAsync(string email, string code, CancellationToken cancellationToken = default)
        => _emailService.SendAsync(
            email,
            "Код восстановления пароля Practice Monitoring",
            $"""
            <p>Ваш код для смены пароля:</p>
            <h2>{WebUtility.HtmlEncode(code)}</h2>
            <p>Код действует 15 минут. Если вы не запрашивали смену пароля, просто игнорируйте это письмо.</p>
            """,
            cancellationToken);

    public Task SendAccountDisabledAsync(User user, CancellationToken cancellationToken = default)
        => _emailService.SendAsync(
            user.Email,
            "Аккаунт Practice Monitoring отключен",
            $"""
            <p>Здравствуйте, {WebUtility.HtmlEncode(user.FullName)}.</p>
            <p>Ваш аккаунт в системе Practice Monitoring был отключен администратором.</p>
            <p>Если это ошибка, обратитесь к администратору системы.</p>
            """,
            cancellationToken);

    public Task SendAccountCreatedAsync(User user, string temporaryPassword, CancellationToken cancellationToken = default)
        => _emailService.SendAsync(
            user.Email,
            "Аккаунт Practice Monitoring создан",
            $"""
            <p>Здравствуйте, {WebUtility.HtmlEncode(user.FullName)}.</p>
            <p>Для вас создан аккаунт в системе Practice Monitoring.</p>
            <p><strong>Email:</strong> {WebUtility.HtmlEncode(user.Email)}</p>
            <p><strong>Первый пароль:</strong> {WebUtility.HtmlEncode(temporaryPassword)}</p>
            <p>При первом входе система попросит заменить этот пароль на постоянный.</p>
            """,
            cancellationToken);
}
