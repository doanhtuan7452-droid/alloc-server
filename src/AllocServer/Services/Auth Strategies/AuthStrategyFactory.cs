using AllocServer.Constants;
using AllocServer.Interfaces.Auth;

namespace AllocServer.Services.Auth_Strategies
{
    /// <summary>
    /// Simple Factory Pattern — Trả về IAuthenticationStrategy tương ứng
    /// dựa trên strategyType (AuthStrategyTypes constants).
    /// Inject tường minh (explicit injection) giống StorageFactory — không dùng IServiceProvider.
    /// </summary>
    public class AuthStrategyFactory : IAuthStrategyFactory
    {
        private readonly LocalLoginStrategy _localLoginStrategy;
        private readonly LocalRegisterStrategy _localRegisterStrategy;
        private readonly GoogleAuthStrategy _googleAuthStrategy;

        public AuthStrategyFactory(
            LocalLoginStrategy localLoginStrategy,
            LocalRegisterStrategy localRegisterStrategy,
            GoogleAuthStrategy googleAuthStrategy)
        {
            _localLoginStrategy = localLoginStrategy;
            _localRegisterStrategy = localRegisterStrategy;
            _googleAuthStrategy = googleAuthStrategy;
        }

        public IAuthenticationStrategy GetStrategy(string strategyType)
        {
            return strategyType switch
            {
                AuthStrategyTypes.LocalLogin => _localLoginStrategy,
                AuthStrategyTypes.LocalRegister => _localRegisterStrategy,
                AuthStrategyTypes.GoogleAuth => _googleAuthStrategy,
                _ => throw new NotSupportedException(
                    $"Authentication strategy '{strategyType}' is not supported.")
            };
        }
    }
}
