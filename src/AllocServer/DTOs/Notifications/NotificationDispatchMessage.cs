namespace AllocServer.DTOs.Notifications
{
    public class NotificationDispatchMessage
    {
        public int RecipientID { get; set; }
        public NotificationDTO Payload { get; set; } = null!;
    }
}
