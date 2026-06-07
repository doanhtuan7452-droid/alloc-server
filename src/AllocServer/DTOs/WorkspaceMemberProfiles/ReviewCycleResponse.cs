using System;

namespace AllocServer.DTOs.WorkspaceMemberProfiles
{
    public class ReviewCycleResponse
    {
        public int CycleID { get; set; }
        public int WorkspaceID { get; set; }
        public string CycleName { get; set; } = null!;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public string Status { get; set; } = null!;
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
