using System;
using System.Collections.Generic;
using AllocServer.DTOs.Accounts;

namespace AllocServer.DTOs.SystemAdmin
{
    public class AccountAdminDetailResponse
    {
        public int AccountId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string AuthType { get; set; } = string.Empty;
        public bool? IsEmailVerified { get; set; }
        public string AccountStatus { get; set; } = string.Empty;
        public bool? IsSystemAccount { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public ResourceProfileResponse? Profile { get; set; }
        public List<WorkspaceMemberListItemDto> Workspaces { get; set; } = new();
    }

    public class WorkspaceMemberListItemDto
    {
        public int WorkspaceId { get; set; }
        public string WorkspaceName { get; set; } = string.Empty;
        public string WorkspaceType { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string MemberStatus { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
    }
}
