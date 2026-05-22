using System.ComponentModel.DataAnnotations;

namespace AllocServer.DTOs.Notifications
{
    public class GetNotificationsQuery
    {
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 20;

        public bool? IsRead { get; set; }
        public int? WorkspaceId { get; set; }
        public string? ReferenceType { get; set; }
    }
}
