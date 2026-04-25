using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PracticeMonitoring.Api.Data;
using PracticeMonitoring.Api.Dtos;
using PracticeMonitoring.Api.Entities;

namespace PracticeMonitoring.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatsController : ControllerBase
{
    private const long MaxAttachmentSizeBytes = 10 * 1024 * 1024;
    private readonly AppDbContext _context;

    public ChatsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("threads")]
    public async Task<ActionResult<List<ChatThreadListItemResponse>>> GetThreads()
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized();

        var rawItems = await _context.ChatThreads
            .AsNoTracking()
            .Where(x => x.Participants.Any(p => p.UserId == currentUserId.Value) && x.Messages.Any())
            .Select(x => new
            {
                x.Id,
                Participants = x.Participants.Select(p => new
                {
                    p.UserId,
                    p.LastReadAtUtc,
                    User = new
                    {
                        p.User.FullName,
                        p.User.Email,
                        Role = p.User.Role.Name,
                        p.User.AvatarUrl,
                        GroupName = p.User.Group != null ? p.User.Group.Name : null,
                        SpecialtyCode = p.User.Group != null && p.User.Group.Specialty != null ? p.User.Group.Specialty.Code : null,
                        SpecialtyName = p.User.Group != null && p.User.Group.Specialty != null ? p.User.Group.Specialty.Name : null
                    }
                }).ToList(),
                LastMessage = x.Messages
                    .OrderByDescending(m => m.CreatedAtUtc)
                    .Select(m => new
                    {
                        m.Id,
                        m.Text,
                        m.CreatedAtUtc,
                        m.SenderUserId,
                        AttachmentsCount = m.Attachments.Count
                    })
                    .FirstOrDefault()
            })
            .ToListAsync();

        var items = rawItems
            .Select(item =>
            {
                var ownParticipant = item.Participants.First(x => x.UserId == currentUserId.Value);
                var otherParticipant = item.Participants.First(x => x.UserId != currentUserId.Value);

                var unreadCount = item.LastMessage is null
                    ? 0
                    : item.LastMessage.SenderUserId == currentUserId.Value
                        ? 0
                        : _context.ChatMessages.Count(m =>
                            m.ChatThreadId == item.Id &&
                            m.SenderUserId != currentUserId.Value &&
                            (ownParticipant.LastReadAtUtc == null || m.CreatedAtUtc > ownParticipant.LastReadAtUtc.Value));

                return new ChatThreadListItemResponse
                {
                    Id = item.Id,
                    OtherUser = new ChatUserShortResponse
                    {
                        Id = otherParticipant.UserId,
                        FullName = otherParticipant.User.FullName,
                        Email = otherParticipant.User.Email,
                        Role = otherParticipant.User.Role,
                        AvatarUrl = otherParticipant.User.AvatarUrl,
                        Subtitle = BuildSubtitle(
                            otherParticipant.User.Role,
                            otherParticipant.User.GroupName,
                            otherParticipant.User.SpecialtyCode,
                            otherParticipant.User.SpecialtyName)
                    },
                    LastMessagePreview = BuildLastMessagePreview(item.LastMessage?.Text, item.LastMessage?.AttachmentsCount ?? 0),
                    LastMessageAtUtc = item.LastMessage?.CreatedAtUtc,
                    UnreadCount = unreadCount
                };
            })
            .OrderByDescending(x => x.LastMessageAtUtc ?? DateTime.MinValue)
            .ToList();

        return Ok(items);
    }

    [HttpGet("contacts/search")]
    public async Task<ActionResult<List<ChatUserShortResponse>>> SearchContacts([FromQuery] string? query = null)
    {
        var currentUserId = GetCurrentUserId();
        var currentRole = GetCurrentUserRole();

        if (currentUserId is null || string.IsNullOrWhiteSpace(currentRole))
            return Unauthorized();

        var accessibleUserIds = await GetAccessibleContactIdsAsync(currentUserId.Value, currentRole);
        var normalizedQuery = (query ?? string.Empty).Trim().ToLower();

        var usersQuery = _context.Users
            .AsNoTracking()
            .Include(x => x.Role)
            .Include(x => x.Group)
                .ThenInclude(x => x.Specialty)
            .Where(x => accessibleUserIds.Contains(x.Id));

        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            usersQuery = usersQuery.Where(x =>
                x.FullName.ToLower().Contains(normalizedQuery) ||
                x.Email.ToLower().Contains(normalizedQuery) ||
                (x.Group != null && x.Group.Name.ToLower().Contains(normalizedQuery)) ||
                (x.Group != null && x.Group.Specialty != null &&
                    (x.Group.Specialty.Code.ToLower().Contains(normalizedQuery) ||
                     x.Group.Specialty.Name.ToLower().Contains(normalizedQuery))));
        }

        var users = await usersQuery
            .OrderBy(x => x.FullName)
            .Take(30)
            .Select(x => new ChatUserShortResponse
            {
                Id = x.Id,
                FullName = x.FullName,
                Email = x.Email,
                Role = x.Role.Name,
                AvatarUrl = x.AvatarUrl,
                Subtitle = BuildSubtitle(
                    x.Role.Name,
                    x.Group != null ? x.Group.Name : null,
                    x.Group != null && x.Group.Specialty != null ? x.Group.Specialty.Code : null,
                    x.Group != null && x.Group.Specialty != null ? x.Group.Specialty.Name : null)
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost("threads")]
    public async Task<ActionResult<ChatThreadListItemResponse>> StartThread([FromBody] StartChatThreadRequest request)
    {
        var currentUserId = GetCurrentUserId();
        var currentRole = GetCurrentUserRole();
        if (currentUserId is null || string.IsNullOrWhiteSpace(currentRole))
            return Unauthorized();

        if (request.TargetUserId <= 0 || request.TargetUserId == currentUserId.Value)
            return BadRequest(new { message = "Выберите корректного собеседника." });

        var targetUser = await _context.Users
            .AsNoTracking()
            .Include(x => x.Role)
            .Include(x => x.Group)
                .ThenInclude(x => x.Specialty)
            .FirstOrDefaultAsync(x => x.Id == request.TargetUserId && x.IsActive);

        if (targetUser is null)
            return NotFound(new { message = "Пользователь не найден." });

        var accessibleIds = await GetAccessibleContactIdsAsync(currentUserId.Value, currentRole);
        if (!accessibleIds.Contains(request.TargetUserId))
            return Forbid();

        var existingThread = await _context.ChatThreads
            .AsNoTracking()
            .Where(x => x.Participants.Count == 2 &&
                        x.Participants.Any(p => p.UserId == currentUserId.Value) &&
                        x.Participants.Any(p => p.UserId == request.TargetUserId))
            .Select(x => x.Id)
            .FirstOrDefaultAsync();

        return Ok(new ChatThreadListItemResponse
        {
            Id = existingThread,
            OtherUser = new ChatUserShortResponse
            {
                Id = targetUser.Id,
                FullName = targetUser.FullName,
                Email = targetUser.Email,
                Role = targetUser.Role.Name,
                AvatarUrl = targetUser.AvatarUrl,
                Subtitle = BuildSubtitle(
                    targetUser.Role.Name,
                    targetUser.Group?.Name,
                    targetUser.Group?.Specialty?.Code,
                    targetUser.Group?.Specialty?.Name)
            }
        });
    }

    [HttpGet("threads/{id:int}")]
    public async Task<ActionResult<ChatThreadDetailsResponse>> GetThread(int id)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized();

        var thread = await _context.ChatThreads
            .Include(x => x.Participants)
                .ThenInclude(x => x.User)
                    .ThenInclude(x => x.Role)
            .Include(x => x.Participants)
                .ThenInclude(x => x.User)
                    .ThenInclude(x => x.Group)
                        .ThenInclude(x => x.Specialty)
            .Include(x => x.Messages.OrderBy(m => m.CreatedAtUtc))
                .ThenInclude(x => x.SenderUser)
            .Include(x => x.Messages)
                .ThenInclude(x => x.Attachments)
            .FirstOrDefaultAsync(x => x.Id == id && x.Participants.Any(p => p.UserId == currentUserId.Value));

        if (thread is null)
            return NotFound();

        var ownParticipant = thread.Participants.First(x => x.UserId == currentUserId.Value);
        ownParticipant.LastReadAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var otherParticipant = thread.Participants.First(x => x.UserId != currentUserId.Value);

        return Ok(new ChatThreadDetailsResponse
        {
            Id = thread.Id,
            OtherUser = new ChatUserShortResponse
            {
                Id = otherParticipant.UserId,
                FullName = otherParticipant.User.FullName,
                Email = otherParticipant.User.Email,
                Role = otherParticipant.User.Role.Name,
                AvatarUrl = otherParticipant.User.AvatarUrl,
                Subtitle = BuildSubtitle(
                    otherParticipant.User.Role.Name,
                    otherParticipant.User.Group?.Name,
                    otherParticipant.User.Group?.Specialty?.Code,
                    otherParticipant.User.Group?.Specialty?.Name)
            },
            Messages = thread.Messages
                .OrderBy(x => x.CreatedAtUtc)
                .Select(x => new ChatMessageResponse
                {
                    Id = x.Id,
                    ThreadId = x.ChatThreadId,
                    SenderUserId = x.SenderUserId,
                    SenderFullName = x.SenderUser.FullName,
                    Text = x.Text,
                    CreatedAtUtc = x.CreatedAtUtc,
                    Attachments = x.Attachments
                        .OrderBy(a => a.FileName)
                        .Select(a => new ChatAttachmentResponse
                        {
                            Id = a.Id,
                            FileName = a.FileName,
                            ContentType = a.ContentType,
                            SizeBytes = a.SizeBytes
                        })
                        .ToList()
                })
                .ToList()
        });
    }

    [HttpPost("threads/{id:int}/messages")]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<ActionResult<ChatMessageResponse>> SendMessage(
        int id,
        [FromForm] int? targetUserId,
        [FromForm] string? text,
        [FromForm] List<IFormFile>? attachments)
    {
        var currentUserId = GetCurrentUserId();
        var currentRole = GetCurrentUserRole();

        if (currentUserId is null || string.IsNullOrWhiteSpace(currentRole))
            return Unauthorized();

        var normalizedText = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        var hasFiles = attachments is not null && attachments.Count > 0;
        if (normalizedText is null && !hasFiles)
            return BadRequest(new { message = "Введите текст сообщения или прикрепите файл." });

        if (attachments is not null && attachments.Any(x => x.Length > MaxAttachmentSizeBytes))
            return BadRequest(new { message = "Один из файлов превышает ограничение 10 МБ." });

        ChatThread? thread;

        if (id > 0)
        {
            thread = await _context.ChatThreads
                .Include(x => x.Participants)
                .FirstOrDefaultAsync(x => x.Id == id && x.Participants.Any(p => p.UserId == currentUserId.Value));

            if (thread is null)
                return NotFound(new { message = "Диалог не найден." });
        }
        else
        {
            if (!targetUserId.HasValue || targetUserId.Value <= 0 || targetUserId.Value == currentUserId.Value)
                return BadRequest(new { message = "Выберите корректного собеседника." });

            var accessibleIds = await GetAccessibleContactIdsAsync(currentUserId.Value, currentRole);
            if (!accessibleIds.Contains(targetUserId.Value))
                return Forbid();

            thread = await _context.ChatThreads
                .Include(x => x.Participants)
                .FirstOrDefaultAsync(x =>
                    x.Participants.Count == 2 &&
                    x.Participants.Any(p => p.UserId == currentUserId.Value) &&
                    x.Participants.Any(p => p.UserId == targetUserId.Value));

            if (thread is null)
            {
                var createdAtUtc = DateTime.UtcNow;
                thread = new ChatThread
                {
                    CreatedAtUtc = createdAtUtc,
                    Participants =
                    {
                        new ChatParticipant
                        {
                            UserId = currentUserId.Value,
                            JoinedAtUtc = createdAtUtc,
                            LastReadAtUtc = createdAtUtc
                        },
                        new ChatParticipant
                        {
                            UserId = targetUserId.Value,
                            JoinedAtUtc = createdAtUtc
                        }
                    }
                };

                _context.ChatThreads.Add(thread);
            }
        }

        var now = DateTime.UtcNow;
        var message = new ChatMessage
        {
            ChatThread = thread,
            SenderUserId = currentUserId.Value,
            Text = normalizedText,
            CreatedAtUtc = now
        };

        if (attachments is not null)
        {
            foreach (var attachment in attachments.Where(x => x.Length > 0))
            {
                await using var stream = attachment.OpenReadStream();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);

                message.Attachments.Add(new ChatMessageAttachment
                {
                    FileName = Path.GetFileName(attachment.FileName),
                    ContentType = string.IsNullOrWhiteSpace(attachment.ContentType) ? "application/octet-stream" : attachment.ContentType,
                    SizeBytes = attachment.Length,
                    Content = memoryStream.ToArray()
                });
            }
        }

        _context.ChatMessages.Add(message);

        var senderParticipant = thread.Participants.First(x => x.UserId == currentUserId.Value);
        senderParticipant.LastReadAtUtc = now;

        await _context.SaveChangesAsync();

        var senderFullName = await _context.Users
            .Where(x => x.Id == currentUserId.Value)
            .Select(x => x.FullName)
            .FirstAsync();

        return Ok(new ChatMessageResponse
        {
            Id = message.Id,
            ThreadId = message.ChatThreadId,
            SenderUserId = message.SenderUserId,
            SenderFullName = senderFullName,
            Text = message.Text,
            CreatedAtUtc = message.CreatedAtUtc,
            Attachments = message.Attachments
                .Select(x => new ChatAttachmentResponse
                {
                    Id = x.Id,
                    FileName = x.FileName,
                    ContentType = x.ContentType,
                    SizeBytes = x.SizeBytes
                })
                .ToList()
        });
    }

    [HttpGet("attachments/{id:int}/download")]
    public async Task<IActionResult> DownloadAttachment(int id)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized();

        var attachment = await _context.ChatMessageAttachments
            .Include(x => x.ChatMessage)
                .ThenInclude(x => x.ChatThread)
                    .ThenInclude(x => x.Participants)
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.ChatMessage.ChatThread.Participants.Any(p => p.UserId == currentUserId.Value));

        if (attachment is null)
            return NotFound();

        return File(attachment.Content, attachment.ContentType, attachment.FileName);
    }

    private int? GetCurrentUserId()
    {
        var rawValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(rawValue, out var userId) ? userId : null;
    }

    private string? GetCurrentUserRole() => User.FindFirstValue(ClaimTypes.Role);

    private async Task<HashSet<int>> GetAccessibleContactIdsAsync(int currentUserId, string currentRole)
    {
        if (currentRole != "Student")
        {
            return await _context.Users
                .AsNoTracking()
                .Where(x => x.IsActive && x.Id != currentUserId)
                .Select(x => x.Id)
                .ToHashSetAsync();
        }

        var supervisorIds = await _context.Users
            .AsNoTracking()
            .Where(x => x.IsActive && x.Id != currentUserId && x.Role.Name == "Supervisor")
            .Select(x => x.Id)
            .ToListAsync();

        var existingThreadUserIds = await _context.ChatThreads
            .AsNoTracking()
            .Where(x => x.Participants.Any(p => p.UserId == currentUserId))
            .SelectMany(x => x.Participants.Where(p => p.UserId != currentUserId).Select(p => p.UserId))
            .Distinct()
            .ToListAsync();

        return supervisorIds.Concat(existingThreadUserIds).ToHashSet();
    }

    private static string BuildLastMessagePreview(string? text, int attachmentsCount)
    {
        if (!string.IsNullOrWhiteSpace(text))
            return text.Length > 80 ? $"{text[..77]}..." : text;

        return attachmentsCount switch
        {
            <= 0 => "Диалог создан",
            1 => "Файл",
            _ => $"Файлы: {attachmentsCount}"
        };
    }

    private static string BuildSubtitle(string role, string? groupName, string? specialtyCode, string? specialtyName)
    {
        return role switch
        {
            "Student" => string.IsNullOrWhiteSpace(groupName)
                ? "Студент"
                : $"Студент группы {groupName}",
            "Supervisor" => "Руководитель практики",
            "DepartmentStaff" => "Работник отдела",
            "Admin" => "Администратор",
            _ => $"{specialtyCode} {specialtyName}".Trim()
        };
    }
}

