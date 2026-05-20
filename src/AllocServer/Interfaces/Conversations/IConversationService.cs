using AllocServer.DTOs.Conversations;
using AllocServer.DTOs.Messages;

namespace AllocServer.Interfaces.Conversations
{
    public interface IConversationService
    {
        Task<ConversationDetailResponse> CreateConversationAsync(int accountId, int workspaceId, CreateConversationRequest request);
        Task<List<ConversationListItemResponse>> GetWorkspaceConversationsAsync(int accountId, int workspaceId);
        Task<ConversationDetailResponse> GetConversationDetailsAsync(int accountId, int conversationId);
        Task<List<MessageResponse>> GetConversationMessagesAsync(int accountId, int conversationId, GetConversationMessagesQuery query);
        Task<MessageResponse> SendMessageAsync(int accountId, int conversationId, CreateMessageRequest request);
        Task MarkConversationAsReadAsync(int accountId, int conversationId);
        Task<ConversationDetailResponse> RenameConversationAsync(int accountId, int conversationId, UpdateConversationNameRequest request);
        Task DeleteConversationAsync(int accountId, int conversationId);
        Task<ConversationDetailResponse> AddMembersAsync(int accountId, int conversationId, AddConversationMembersRequest request);
        Task RemoveMemberAsync(int accountId, int conversationId, int targetMemberId);
    }
}
