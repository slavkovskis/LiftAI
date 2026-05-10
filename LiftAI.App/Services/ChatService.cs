using System.Net.Http.Json;
using LiftAI.Shared.Models.Dtos.Chat;
using LiftAI.Shared.Models.Dtos.Conversation;
using LiftAI.Shared.Models.Enums;

namespace LiftAI.App.Services;

public class ChatService
{
    private readonly HttpClient _httpClient;

    public ChatService(IHttpClientFactory clientFactory)
    {
        _httpClient = clientFactory.CreateClient("Api");
    }

    public async Task<List<ChatConversationDto>> GetConversationsAsync()
    {
        var response = await _httpClient.GetAsync("api/chat/conversations");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ChatConversationDto>>() ?? new List<ChatConversationDto>();
    }

    public async Task<ChatConversationDetailDto?> GetConversationAsync(int id)
    {
        var response = await _httpClient.GetAsync($"api/chat/conversations/{id}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ChatConversationDetailDto>();
    }

    public async Task<ChatResponseDto?> SendMessageAsync(int? conversationId, IEnumerable<ChatMessageDto> messages, ContextMode contextMode)
    {
        var request = new ChatRequestDto
        {
            ConversationId = conversationId,
            Messages = messages.ToList(),
            ContextMode = contextMode
        };

        var response = await _httpClient.PostAsJsonAsync("api/chat", request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            return new ChatResponseDto
            {
                Success = false,
                ConversationId = conversationId ?? 0,
                Response = string.Empty,
                IsQuotaLimited = false,
                RemainingRequestsToday = null,
                Message = $"HTTP {(int)response.StatusCode}: {body}"
            };
        }

        return await response.Content.ReadFromJsonAsync<ChatResponseDto>();
    }

    public async Task<ChatQuotaStatusDto?> GetQuotaStatusAsync()
    {
        var response = await _httpClient.GetAsync("api/chat/quota");
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<ChatQuotaStatusDto>();
    }

    public async Task DeleteConversationAsync(int conversationId)
    {
        var response = await _httpClient.DeleteAsync($"api/chat/conversations/{conversationId}");
        response.EnsureSuccessStatusCode();
    }
}