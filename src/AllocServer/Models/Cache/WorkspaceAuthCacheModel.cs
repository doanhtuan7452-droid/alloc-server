namespace AllocServer.Models.Cache
{
    public class WorkspaceAuthCacheModel
    {
        public int WorkspaceRoleID { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public List<string> Permissions { get; set; } = new();
    }
}
