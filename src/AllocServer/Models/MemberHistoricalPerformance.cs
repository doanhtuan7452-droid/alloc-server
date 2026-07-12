namespace AllocServer.Models
{
    public class MemberHistoricalPerformance
    {
        public int WorkspaceMemberID { get; set; }
        public int TotalCompletedTasks { get; set; }
        public decimal? PreviousTaskSuccessRate { get; set; }
        public decimal? EfficiencyRatio { get; set; }
    }
}
