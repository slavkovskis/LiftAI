using System.Security.Claims;
using LiftAI.Api.Services.Conversation;

namespace LiftAI.Api.Endpoints;

public static class ChatConversationEndpoints
{
    public static void MapChatConversationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/chat/conversations").RequireAuthorization();
        
        group.MapGet("/", async (ClaimsPrincipal claim, IConversationService conversationService, CancellationToken ct) =>
        {
            var userId = claim.FindFirstValue(ClaimTypes.NameIdentifier);
            
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Results.Unauthorized();
            }

            var conversations = await conversationService.GetConversationsAsync(userId, ct);
            return Results.Ok(conversations);
        })
        .WithTags("Chat")
        .WithName("GetChatConversations");

        group.MapGet("/{id:int}", async (ClaimsPrincipal claim, IConversationService conversationService, int id, CancellationToken ct) =>
        {
            var userId = claim.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Results.Unauthorized();
            }

            var conversation = await conversationService.GetConversationAsync(userId, id, ct);
            return conversation is null ? Results.NotFound() : Results.Ok(conversation);
        })
        .WithTags("Chat")
        .WithName("GetChatConversationById");

        group.MapDelete("/{id:int}", async (ClaimsPrincipal claim, IConversationService conversationService, int id, CancellationToken ct) =>
        {
            var userId = claim.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Results.Unauthorized();
            }

            var deleted = await conversationService.DeleteConversationAsync(userId, id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithTags("Chat")
        .WithName("DeleteChatConversation");
    }
}