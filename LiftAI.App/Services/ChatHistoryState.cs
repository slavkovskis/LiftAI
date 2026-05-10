using LiftAI.Shared.Models.Dtos.Chat;
using LiftAI.Shared.Models.Dtos.Conversation;

namespace LiftAI.App.Services;

public class ChatHistoryState
{
    private readonly ChatService _chatService;
    private List<ChatConversationDto> _conversations = new();
    private string _searchTerm = "";

    public ChatConversationDetailDto? SelectedConversation { get; private set; }
    public int? SelectedConversationId => SelectedConversation?.Id;
    public IEnumerable<ChatConversationDto> FilteredConversations =>
        _conversations.Where(c => c.Title.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase));

    public event EventHandler? ConversationsChanged;
    public event EventHandler? SelectedConversationChanged;

    public ChatHistoryState(ChatService chatService)
    {
        _chatService = chatService;
    }

    public async Task LoadConversationsAsync()
    {
        _conversations = await _chatService.GetConversationsAsync();
        ConversationsChanged?.Invoke(this, EventArgs.Empty);

        if (SelectedConversation is null && _conversations.Count > 0)
        {
            await SetSelectedConversationAsync(_conversations[0].Id);
        }
    }

    public void SetSearchTerm(string term)
    {
        _searchTerm = term;
        ConversationsChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task SetSelectedConversationAsync(int conversationId)
    {
        if (SelectedConversation?.Id == conversationId) return;

        SelectedConversation = await _chatService.GetConversationAsync(conversationId);
        SelectedConversationChanged?.Invoke(this, EventArgs.Empty);
    }

    public void StartNewConversation()
    {
        SelectedConversation = new ChatConversationDetailDto
        {
            Id = 0,
            Title = "New conversation",
            Messages = new List<ChatMessageDto>(),
            LastMessageAt = DateTime.UtcNow
        };

        SelectedConversationChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task DeleteConversationAsync(int conversationId)
    {
        if (conversationId <= 0)
        {
            return;
        }

        await _chatService.DeleteConversationAsync(conversationId);
        _conversations.RemoveAll(c => c.Id == conversationId);
        ConversationsChanged?.Invoke(this, EventArgs.Empty);

        if (SelectedConversation?.Id == conversationId)
        {
            if (_conversations.Count > 0)
            {
                await SetSelectedConversationAsync(_conversations[0].Id);
            }
            else
            {
                StartNewConversation();
            }
        }
    }

    public async Task RefreshConversationsAsync(int? preferredConversationId = null)
    {
        await LoadConversationsAsync();

        if (preferredConversationId.HasValue)
        {
            await SetSelectedConversationAsync(preferredConversationId.Value);
        }
    }
}

