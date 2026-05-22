using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Workspaces
{
    public class WorkspaceMemberSummaryResponse
    {
        [JsonPropertyName("totalMembers")]
        public int TotalMembers { get; set; }

        [JsonPropertyName("activeMembers")]
        public int ActiveMembers { get; set; }

        [JsonPropertyName("pendingInvites")]
        public int PendingInvites { get; set; }

        [JsonPropertyName("deactivatedMembers")]
        public int DeactivatedMembers { get; set; }
    }
}
