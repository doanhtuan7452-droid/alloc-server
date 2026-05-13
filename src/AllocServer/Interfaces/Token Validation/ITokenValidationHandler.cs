using AllocServer.Contexts;
using AllocServer.DTOs.Auth;
using AllocServer.Models;

namespace AllocServer.Interfaces.TokenValidation
{
    /// <summary>
    /// Chain of Responsibility Pattern — Interface cho mỗi Handler trong chuỗi xác thực Refresh Token.
    /// Mỗi Handler chỉ chịu trách nhiệm cho MỘT bước kiểm tra duy nhất.
    /// Nếu pass → gọi next handler. Nếu fail → trả về lỗi ngay, không tiếp tục.
    /// </summary>
    public interface ITokenValidationHandler
    {
        /// <summary>
        /// Thiết lập handler kế tiếp trong chuỗi. Trả về handler kế tiếp để hỗ trợ fluent chaining:
        /// <code>handlerA.SetNext(handlerB).SetNext(handlerC)</code>
        /// </summary>
        ITokenValidationHandler SetNext(ITokenValidationHandler next);

        /// <summary>Thực hiện bước xác thực của handler này với context được cung cấp</summary>
        Task<TokenValidationResult> HandleAsync(TokenValidationContext context);
    }
}
