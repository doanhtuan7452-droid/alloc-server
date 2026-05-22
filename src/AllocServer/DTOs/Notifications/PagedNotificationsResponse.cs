namespace AllocServer.DTOs.Notifications
{
    public class PagedNotificationsResponse
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public int UnreadCount { get; set; }
        public List<NotificationDTO> Items { get; set; } = new();
    }
}
