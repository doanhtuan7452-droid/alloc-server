namespace AllocServer.Interfaces.Notifications
{
    public interface IFirebasePushService
    {
        Task<(bool IsSuccess, string? ErrorMessage)> SendPushNotificationAsync(string deviceToken, string title, string body, string referenceType, int referenceId);
    }
}
