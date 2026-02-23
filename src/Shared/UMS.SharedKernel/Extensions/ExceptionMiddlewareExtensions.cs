using Microsoft.AspNetCore.Builder;
using UMS.SharedKernel.Middleware;

namespace UMS.SharedKernel.Extensions;

public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        => app.UseMiddleware<GlobalExceptionMiddleware>();
}
