namespace AllocServer.Interfaces.Auth
{
    public interface IAvatarGenerationService
    {
        string GenerateAvatarUrl(string? name, string email);
        bool IsGeneratedAvatar(string? url);
    }
}
