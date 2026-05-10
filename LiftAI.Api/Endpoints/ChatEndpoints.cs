using System.Security.Claims;
using LiftAI.Api.Data;
using LiftAI.Api.Services.Chat;
using LiftAI.Api.Services.Conversation;
using LiftAI.Api.Services.Ollama;
using LiftAI.Shared.Models.Dtos.Chat;
using LiftAI.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LiftAI.Api.Endpoints;

public static class ChatEndpoints
{
    private const int FreeDailyChatLimit = 3;

    public static void MapChatEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/chat").RequireAuthorization();

        group.MapGet("/quota", async (
                ClaimsPrincipal claim,
                ApplicationDbContext db,
                CancellationToken ct) =>
            {
                var userId = claim.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Results.Unauthorized();
                }

                var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
                if (user is null)
                {
                    return Results.Unauthorized();
                }

                var didReset = false;
                var todayUtc = DateTime.UtcNow.Date;

                if (user.ChatDailyLastResetUtc is null || user.ChatDailyLastResetUtc.Value.Date < todayUtc)
                {
                    user.ChatDailyCount = 0;
                    user.ChatDailyLastResetUtc = todayUtc;
                    didReset = true;
                }

                if (didReset)
                {
                    await db.SaveChangesAsync(ct);
                }

                var isPremium = user.IsPremium;
                var remaining = isPremium ? (int?)null : Math.Max(0, FreeDailyChatLimit - user.ChatDailyCount);

                return Results.Ok(new ChatQuotaStatusDto
                {
                    IsPremium = isPremium,
                    IsQuotaLimited = !isPremium && remaining == 0,
                    RemainingRequestsToday = remaining
                });
            })
            .WithTags("Chat")
            .WithName("GetChatQuotaStatus");

        group.MapPost("/", async (
                ChatRequestDto dto,
                ClaimsPrincipal claim,
                ApplicationDbContext db,
                IConversationService conversationService,
                ChatContextBuilder contextBuilder,
                IOllamaChatClient ollamaChatClient,
                CancellationToken ct) =>
            {
                var userId = claim.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Results.Unauthorized();
                }

                if (dto.Messages.Count == 0)
                {
                    return Results.BadRequest("At least one chat message is required.");
                }

                var lastMessage = dto.Messages[^1];

                if (lastMessage.Role != ChatRole.User || string.IsNullOrWhiteSpace(lastMessage.Content))
                {
                    return Results.BadRequest("The final message must be a non-empty user message.");
                }

                var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
                if (user is null)
                {
                    return Results.Unauthorized();
                }

                var didReset = false;
                var todayUtc = DateTime.UtcNow.Date;

                if (user.ChatDailyLastResetUtc is null || user.ChatDailyLastResetUtc.Value.Date < todayUtc)
                {
                    user.ChatDailyCount = 0;
                    user.ChatDailyLastResetUtc = todayUtc;
                    didReset = true;
                }

                var isPremium = user.IsPremium;

                switch (isPremium)
                {
                    case false when user.ChatDailyCount >= FreeDailyChatLimit:
                        return Results.Ok(new ChatResponseDto
                        {
                            Success = false,
                            ConversationId = dto.ConversationId ?? 0,
                            Response = string.Empty,
                            IsQuotaLimited = true,
                            RemainingRequestsToday = 0,
                            Message = "Daily chat limit reached. Try again tomorrow."
                        });
                    case false:
                        user.ChatDailyCount += 1;
                        didReset = true;
                        break;
                }

                if (didReset)
                {
                    await db.SaveChangesAsync(ct);
                }

                int conversationId;

                try
                {
                    conversationId = await conversationService.EnsureConversationAsync(userId, dto.ConversationId, ct);
                }
                catch (InvalidOperationException)
                {
                    return Results.NotFound("Conversation not found.");
                }

                var remainingForToday = isPremium
                    ? (int?)null
                    : Math.Max(0, FreeDailyChatLimit - user.ChatDailyCount);

                await conversationService.SaveMessageAsync(conversationId, ChatRole.User, lastMessage.Content, ct);

                var effectiveContextMode = isPremium ? dto.ContextMode : ContextMode.Normal;

                var systemPrompt =
                    await contextBuilder.BuildSystemPromptAsync(userId, effectiveContextMode, conversationId, ct);

                var messagesForModel = new List<ChatMessageDto>
                {
                    new()
                    {
                        Role = ChatRole.System,
                        Content = systemPrompt
                    }
                };

                messagesForModel.AddRange(dto.Messages);

                var assistantResponse = await ollamaChatClient.SendAsync(messagesForModel, ct);

                if (string.IsNullOrWhiteSpace(assistantResponse))
                {
                    return Results.Ok(new ChatResponseDto
                    {
                        Success = false,
                        ConversationId = conversationId,
                        Response = string.Empty,
                        IsQuotaLimited = false,
                        RemainingRequestsToday = remainingForToday,
                        Message = "Could not get a response. Please try again."
                    });
                }

                await conversationService.SaveMessageAsync(conversationId, ChatRole.Assistant, assistantResponse, ct);

                return Results.Ok(new ChatResponseDto
                {
                    Success = true,
                    ConversationId = conversationId,
                    Response = assistantResponse,
                    IsQuotaLimited = false,
                    RemainingRequestsToday = remainingForToday,
                    Message = "OK"
                });
            })
            .WithTags("Chat")
            .WithName("SendChatMessage");
    }
}