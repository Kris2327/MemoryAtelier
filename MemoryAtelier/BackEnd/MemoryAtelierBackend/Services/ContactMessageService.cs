using Microsoft.EntityFrameworkCore;
using MemoryAtelierBackend.Data;
using MemoryAtelierBackend.DTOs;
using MemoryAtelierBackend.Models;

namespace MemoryAtelierBackend.Services;

public class ContactMessageService(AppDbContext db, EmailService emailService)
{
    public async Task SaveAsync(string name, string email, string messageText)
    {
        db.ContactMessages.Add(new ContactMessage
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            Message = messageText,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    public async Task<List<ContactMessageSummaryDto>> GetAllAsync()
    {
        return await db.ContactMessages
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new ContactMessageSummaryDto(m.Id, m.Name, m.Email, m.Message, m.CreatedAt, m.IsReplied, m.ReplyText, m.RepliedAt))
            .ToListAsync();
    }

    public async Task<bool> ReplyAsync(Guid id, string replyText)
    {
        var contactMessage = await db.ContactMessages.FirstOrDefaultAsync(m => m.Id == id);
        if (contactMessage == null) return false;

        await emailService.SendContactReplyAsync(contactMessage.Name, contactMessage.Email, contactMessage.Message, replyText);

        contactMessage.IsReplied = true;
        contactMessage.ReplyText = replyText;
        contactMessage.RepliedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return true;
    }
}
