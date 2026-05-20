using AllocServer.DTOs.Messages;

namespace AllocServer.Interfaces.Messages
{
    public interface IMessageService
    {
        Task<MessageResponse> EditMessageAsync(int accountId, int messageId, UpdateMessageRequest request);
        Task DeleteMessageAsync(int accountId, int messageId);
    }
}
