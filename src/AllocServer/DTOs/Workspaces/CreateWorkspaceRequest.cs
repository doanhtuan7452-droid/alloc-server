namespace AllocServer.DTOs.Workspaces
{
    public class CreateWorkspaceRequest
    {
        public string Name { get; set; }
        public string Type { get; set; } // 'Personal', 'Company'
    }
}
