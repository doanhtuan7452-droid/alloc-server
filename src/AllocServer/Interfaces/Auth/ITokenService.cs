using AllocServer.Models;

namespace AllocServer.Interfaces.Auth
{
    public interface ITokenService
    {
        string GenerateJwtToken(Account account);
        string GenerateRefreshToken();
    }
}
