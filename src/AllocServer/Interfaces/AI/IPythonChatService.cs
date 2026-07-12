using System.Threading.Tasks;
using AllocServer.DTOs.AIChat;

namespace AllocServer.Interfaces.AI
{
    public interface IPythonChatService
    {
        Task<PythonConversationsResponse> GetConversationsAsync(string userId, int limit, int skip);
        Task<PythonMessagesResponse> GetMessagesAsync(string conversationId, string userId, int limit, int skip, string order);
        Task<PythonChatQueryResponse> ChatAsync(string userId, PythonChatQueryRequest request, System.Threading.CancellationToken cancellationToken = default);
    }
}
