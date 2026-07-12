namespace AllocServer.Models
{
    public class MemberCurrentWorkload
    {
        public int WorkspaceMemberID { get; set; }
        public decimal CurrentWorkloadValue { get; set; }
        public int ActiveTaskCount { get; set; }
    }
}
