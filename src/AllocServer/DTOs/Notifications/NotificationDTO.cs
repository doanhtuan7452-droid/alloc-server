namespace AllocServer.DTOs.Notifications
{
    public class NotificationDTO
    {
        public int NotificationID { get; set; }
        public string NotificationType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Message { get; set; }
        public string ReferenceType { get; set; } = string.Empty;
        public int ReferenceID { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? MetadataJson { get; set; }
        public NotificationReferenceResponse? ReferenceData { get; set; }
    }
}
