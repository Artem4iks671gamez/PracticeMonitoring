using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PracticeMonitoring.Api.Data;
using PracticeMonitoring.Api.Entities;

namespace PracticeMonitoring.Api.Services;

public class EmailVerificationService
{
    public const string RegistrationPurpose = "Registration";
    public const string PasswordResetPurpose = "PasswordReset";

    private const int MaxAttempts = 5;
    private readonly AppDbContext _context;

    public EmailVerificationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<string> CreateCodeAsync(string email, string purpose, string? payloadJson, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        var now = DateTime.UtcNow;

        await _context.EmailVerificationCodes
            .Where(x => x.Email == normalizedEmail && x.Purpose == purpose && x.ConsumedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.ConsumedAtUtc, now),
                cancellationToken);

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

        _context.EmailVerificationCodes.Add(new EmailVerificationCode
        {
            Email = normalizedEmail,
            Purpose = purpose,
            CodeHash = HashCode(normalizedEmail, purpose, code),
            PayloadJson = payloadJson,
            Attempts = 0,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddMinutes(15)
        });

        await _context.SaveChangesAsync(cancellationToken);
        return code;
    }

    public async Task<EmailVerificationCode> VerifyCodeAsync(string email, string purpose, string code, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        var now = DateTime.UtcNow;

        var verification = await _context.EmailVerificationCodes
            .Where(x => x.Email == normalizedEmail && x.Purpose == purpose && x.ConsumedAtUtc == null)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (verification is null)
            throw new InvalidOperationException("Код не найден. Запросите новый код.");

        if (verification.ExpiresAtUtc < now)
            throw new InvalidOperationException("Срок действия кода истёк. Запросите новый код.");

        if (verification.Attempts >= MaxAttempts)
            throw new InvalidOperationException("Превышено количество попыток. Запросите новый код.");

        verification.Attempts++;

        if (verification.CodeHash != HashCode(normalizedEmail, purpose, code.Trim()))
        {
            await _context.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Неверный код подтверждения.");
        }

        verification.ConsumedAtUtc = now;
        await _context.SaveChangesAsync(cancellationToken);

        return verification;
    }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string HashCode(string email, string purpose, string code)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{email}:{purpose}:{code}"));
        return Convert.ToHexString(bytes);
    }
}
