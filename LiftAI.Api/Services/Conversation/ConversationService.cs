using LiftAI.Api.Data;
using LiftAI.Api.Data.Models;
using LiftAI.Api.Services.Ollama;
using LiftAI.Shared.Models.Dtos.Chat;
using LiftAI.Shared.Models.Dtos.Conversation;
using LiftAI.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LiftAI.Api.Services.Conversation;

public class ConversationService(
    ApplicationDbContext db,
    IOllamaChatClient ollamaChatClient,
    ILogger<ConversationService> logger) : IConversationService
{
    private const string DefaultTitle = "New conversation";
    private const int MaxTitleWords = 5;
    private const int MaxTitleCharacters = 60;

    public async Task<int> EnsureConversationAsync(string userId, int? conversationId, CancellationToken ct = default)
    {
        if (conversationId.HasValue)
        {
            var exists = await db.ChatConversations
                .AsNoTracking()
                .AnyAsync(c => c.Id == conversationId.Value && c.UserId == userId, ct);

            if (!exists)
            {
                throw new InvalidOperationException("Conversation not found.");
            }

            return conversationId.Value;
        }

        var conversation = new ChatConversation
        {
            UserId = userId,
            Title = DefaultTitle,
            CreatedAt = DateTime.UtcNow,
            LastMessageAt = DateTime.UtcNow
        };

        db.ChatConversations.Add(conversation);
        await db.SaveChangesAsync(ct);

        return conversation.Id;
    }

    public async Task SaveMessageAsync(int conversationId, ChatRole role, string content, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Message content is required.", nameof(content));
        }

        var conversation = await db.ChatConversations.FirstOrDefaultAsync(c => c.Id == conversationId, ct);

        if (conversation is null)
        {
            throw new InvalidOperationException("Conversation not found.");
        }

        db.ChatMessages.Add(new ChatMessageEntity
        {
            ConversationId = conversationId,
            Role = role,
            Content = content.Trim(),
            CreatedAt = DateTime.UtcNow
        });

        conversation.LastMessageAt = DateTime.UtcNow;
        conversation.UpdatedAt = DateTime.UtcNow;

        // Set title from AI's first response after the conversation is new.
        if (conversation.Title == DefaultTitle && role == ChatRole.Assistant)
        {
            conversation.Title = await BuildTitleAsync(content, ct);
        }

        await db.SaveChangesAsync(ct);
    }


    public async Task<List<ChatConversationDto>> GetConversationsAsync(string userId, CancellationToken ct = default)
    {
        return await db.ChatConversations
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.LastMessageAt)
            .Select(c => new ChatConversationDto
            {
                Id = c.Id,
                Title = c.Title,
                CreatedAt = c.CreatedAt,
                LastMessageAt = c.LastMessageAt
            })
            .ToListAsync(ct);
    }

    public async Task<ChatConversationDetailDto?> GetConversationAsync(string userId, int conversationId, CancellationToken ct = default)
    {
        var conversation = await db.ChatConversations
            .AsNoTracking()
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId, ct);

        if (conversation is null)
        {
            return null;
        }

        return new ChatConversationDetailDto
        {
            Id = conversation.Id,
            Title = conversation.Title,
            LastMessageAt = conversation.LastMessageAt,
            Messages = conversation.Messages
                .OrderBy(m => m.CreatedAt)
                .Select(m => new ChatMessageDto
                {
                    Role = m.Role,
                    Content = m.Content
                })
                .ToList()
        };
    }

    public async Task<bool> DeleteConversationAsync(string userId, int conversationId, CancellationToken ct = default)
    {
        var conversation = await db.ChatConversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId, ct);

        if (conversation is null)
        {
            return false;
        }

        db.ChatConversations.Remove(conversation);
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// Generates a short conversation title from the assistant's reply.
    /// Tries Ollama first; falls back to a simple word-trimmed summary if the
    /// AI call fails or returns an unusable response. Title generation is
    /// best-effort and should never block saving the message.
    /// </summary>
    private async Task<string> BuildTitleAsync(string content, CancellationToken ct)
    {
        try
        {
            var aiTitle = await GenerateTitleWithAiAsync(content, ct);

            if (!string.IsNullOrWhiteSpace(aiTitle))
            {
                return aiTitle;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to generate AI conversation title; falling back to heuristic.");
        }

        return BuildFallbackTitle(content);
    }

    private async Task<string?> GenerateTitleWithAiAsync(string content, CancellationToken ct)
    {
        var prompt = $$"""
            You are generating a very short title that summarizes the topic of the message below.

            RULES:
            - Reply with ONLY the title, nothing else.
            - 3 to {{MaxTitleWords}} words maximum.
            - No quotes, no punctuation at the end, no leading "Title:" prefix, no greeting.
            - Use sentence case: capitalize only the first word and any proper nouns. Do not capitalize every word.

            Examples of correct style:
              "Post-workout nutrition tips"
              "Building a 3-day split"
              "Foam rolling for recovery"

            Message:
            {{content}}
            """;

        var messages = new List<ChatMessageDto>
        {
            new() { Role = ChatRole.User, Content = prompt }
        };

        var response = await ollamaChatClient.SendAsync(messages, ct);

        return CleanAiTitle(response);
    }

    private static string? CleanAiTitle(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var title = raw.Trim();

        // Take only the first non-empty line in case the model rambled.
        var firstLine = title
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(firstLine))
        {
            return null;
        }

        title = firstLine.Trim();

        // Strip a leading "Title:" or "Summary:" prefix that small models love to add.
        foreach (var prefix in new[] { "title:", "summary:", "topic:" })
        {
            if (title.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                title = title[prefix.Length..].Trim();
            }
        }

        // Strip surrounding quotes / asterisks / backticks.
        title = title.Trim('"', '\'', '“', '”', '‘', '’', '*', '`', ' ');

        // Strip trailing punctuation like a stray period or exclamation mark.
        title = title.TrimEnd('.', '!', '?', ',', ';', ':');

        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        // Enforce the word cap defensively in case the model ignored it.
        var words = title.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > MaxTitleWords)
        {
            title = string.Join(' ', words.Take(MaxTitleWords));
        }

        // Hard character cap so a single absurdly long word can't break layout.
        if (title.Length > MaxTitleCharacters)
        {
            title = title[..MaxTitleCharacters].TrimEnd();
        }

        // Force sentence case: lowercase everything, then uppercase the very first letter.
        // This is defensive in case the model still returns Title Case.
        title = ToSentenceCase(title);

        return title;
    }

    private static string ToSentenceCase(string title)
    {
        if (string.IsNullOrEmpty(title))
        {
            return title;
        }

        var lower = title.ToLowerInvariant();

        // Capitalize only the first non-whitespace character.
        var chars = lower.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (!char.IsWhiteSpace(chars[i]))
            {
                chars[i] = char.ToUpperInvariant(chars[i]);
                break;
            }
        }

        return new string(chars);
    }

    private static string BuildFallbackTitle(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return DefaultTitle;
        }

        var words = text.Split(
            [' ', '\t', '\r', '\n', '.', ',', ';', ':', '!', '?'],
            StringSplitOptions.RemoveEmptyEntries);

        if (words.Length == 0)
        {
            return DefaultTitle;
        }

        var title = string.Join(' ', words.Take(MaxTitleWords));

        if (title.Length > MaxTitleCharacters)
        {
            title = title[..MaxTitleCharacters].TrimEnd();
        }

        return ToSentenceCase(title);
    }
}
