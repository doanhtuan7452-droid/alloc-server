using AllocServer.Contexts;
using AllocServer.DTOs.Auth;
using AllocServer.Interfaces.TokenValidation;

namespace AllocServer.Services.Token_Validation_Handlers
{
    /// <summary>
    /// Chain of Responsibility — Abstract Base Class triển khai logic SetNext/PassToNext chung.
    /// Các Handler cụ thể chỉ cần override HandleAsync() và tập trung vào logic xác thực của mình.
    /// </summary>
    public abstract class BaseTokenValidationHandler : ITokenValidationHandler
    {
        private ITokenValidationHandler? _nextHandler;

        /// <summary>
        /// Thiết lập handler tiếp theo và trả về nó để hỗ trợ fluent chaining:
        /// <code>tokenExistsHandler.SetNext(notRevokedHandler).SetNext(notExpiredHandler).SetNext(accountActiveHandler)</code>
        /// </summary>
        public ITokenValidationHandler SetNext(ITokenValidationHandler next)
        {
            _nextHandler = next;
            return next; // Trả về next để chain dạng: A.SetNext(B).SetNext(C)
        }

        /// <summary>Mỗi Handler cụ thể tự implement logic xác thực của mình</summary>
        public abstract Task<TokenValidationResult> HandleAsync(TokenValidationContext context);

        /// <summary>
        /// Được gọi bởi Handler con khi bước xác thực của nó pass.
        /// Nếu còn handler tiếp theo → chuyển sang. Nếu là cuối chain → trả về kết quả thành công.
        /// </summary>
        protected async Task<TokenValidationResult> PassToNextAsync(TokenValidationContext context)
        {
            if (_nextHandler != null)
                return await _nextHandler.HandleAsync(context);

            // Đây là handler cuối cùng trong chain và đã pass
            // Lúc này context.Session và context.Account đều đã được điền đầy đủ
            return TokenValidationResult.Pass(context.Session!, context.Account!);
        }
    }
}
