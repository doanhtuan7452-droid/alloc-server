using AllocServer.DTOs.Notifications;

namespace AllocServer.Interfaces.Notifications
{
    public interface INotificationService
    {
        Task<PagedNotificationsResponse> GetNotificationsAsync(int accountId, GetNotificationsQuery query);
        Task<int> GetUnreadCountAsync(int accountId);
        Task MarkAsReadAsync(int accountId, int notificationId);
        Task MarkAllAsReadAsync(int accountId);
        Task<NotificationDTO> GetNotificationDetailAsync(int accountId, int notificationId);
        Task RegisterDeviceTokenAsync(int accountId, RegisterDeviceTokenRequest request);
        Task RevokeDeviceTokenAsync(int accountId, RevokeDeviceTokenRequest request);
    }
}
