using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Workspaces
{
    public class WorkspaceDetailResponse
    {
        [JsonPropertyName("workspaceId")]
        public int WorkspaceID { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("currentUserMembership")]
        public WorkspaceMembershipResponse CurrentUserMembership { get; set; } = new();

        [JsonPropertyName("memberSummary")]
        public WorkspaceMemberSummaryResponse MemberSummary { get; set; } = new();

        [JsonPropertyName("projectSummary")]
        public WorkspaceProjectSummaryResponse ProjectSummary { get; set; } = new();

        [JsonPropertyName("currentPlan")]
        public WorkspacePlanSummaryResponse? CurrentPlan { get; set; }
    }

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

    public class WorkspaceProjectSummaryResponse
    {
        [JsonPropertyName("totalProjects")]
        public int TotalProjects { get; set; }

        [JsonPropertyName("planningProjects")]
        public int PlanningProjects { get; set; }

        [JsonPropertyName("inProgressProjects")]
        public int InProgressProjects { get; set; }

        [JsonPropertyName("completedProjects")]
        public int CompletedProjects { get; set; }

        [JsonPropertyName("onHoldProjects")]
        public int OnHoldProjects { get; set; }

        [JsonPropertyName("cancelledProjects")]
        public int CancelledProjects { get; set; }
    }

    public class WorkspaceMemberDetailResponse
    {
        [JsonPropertyName("workspaceMemberId")]
        public int WorkspaceMemberID { get; set; }

        [JsonPropertyName("resource")]
        public WorkspaceMemberResourceResponse Resource { get; set; } = new();

        [JsonPropertyName("employeeCode")]
        public string EmployeeCode { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("joinedAt")]
        public DateTime JoinedAt { get; set; }

        [JsonPropertyName("role")]
        public WorkspaceRoleSummaryResponse Role { get; set; } = new();
    }

    public class WorkspaceMemberResourceResponse
    {
        [JsonPropertyName("resourceId")]
        public int ResourceID { get; set; }

        [JsonPropertyName("fullName")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("phoneNumber")]
        public string? PhoneNumber { get; set; }

        [JsonPropertyName("avatarUrl")]
        public string? AvatarURL { get; set; }

        [JsonPropertyName("timezone")]
        public string Timezone { get; set; } = "UTC";
    }

    public class WorkspacePlanSummaryResponse
    {
        [JsonPropertyName("planCode")]
        public string PlanCode { get; set; } = string.Empty;

        [JsonPropertyName("limits")]
        public List<WorkspaceFeatureLimitResponse> Limits { get; set; } = new();
    }

    public class WorkspaceFeatureLimitResponse
    {
        [JsonPropertyName("featureCode")]
        public string FeatureCode { get; set; } = string.Empty;

        [JsonPropertyName("isIncluded")]
        public bool IsIncluded { get; set; }

        [JsonPropertyName("limitValue")]
        public int LimitValue { get; set; }
    }
}
