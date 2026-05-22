namespace AllocServer.DTOs.Notifications
{
    public class NotificationReferenceResponse
    {
        public string Type { get; set; } = string.Empty;
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int? WorkspaceId { get; set; }
        public int? ProjectId { get; set; }
        public string? Url { get; set; }
        public object? Extra { get; set; }
    }
}
