namespace AllocServer.Models
{
    // Keyless Entity mapping to vw_WorkspaceCurrentLimits
    public class WorkspaceCurrentLimit
    {
        public int WorkspaceID { get; set; }
        public string WorkspaceName { get; set; }
        public string PlanCode { get; set; }
        public string FeatureCode { get; set; }
        public bool IsIncluded { get; set; }
        public int LimitValue { get; set; }
    }
}
