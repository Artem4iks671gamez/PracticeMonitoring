using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PracticeMonitoring.Api.Services;

namespace PracticeMonitoring.Api.Controllers;

[ApiController]
[Route("api/email-test")]
[AllowAnonymous]
public class EmailTestController : ControllerBase
{
    private readonly EmailService _emailService;
    private readonly IWebHostEnvironment _environment;

    public EmailTestController(EmailService emailService, IWebHostEnvironment environment)
    {
        _emailService = emailService;
        _environment = environment;
    }

    [HttpPost]
    public async Task<IActionResult> Send(TestEmailRequest request, CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
            return NotFound();

        await _emailService.SendAsync(
            request.ToEmail,
            "Practice Monitoring SMTP test",
            "<h1>SMTP is configured</h1><p>This test email was sent from PracticeMonitoring.Api.</p>",
            cancellationToken);

        return Ok(new { message = "Test email sent." });
    }
}

public record TestEmailRequest(string ToEmail);
