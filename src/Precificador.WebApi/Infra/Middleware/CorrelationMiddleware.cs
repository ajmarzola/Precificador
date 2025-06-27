using Microsoft.Extensions.Primitives;

namespace Precificador.WebApi.Infra.Middleware
{
    public class CorrelationMiddleware(RequestDelegate next)
    {
        private readonly RequestDelegate _next = next;
        private const string _correlationIdHeader = "x-correlation-id";

        public async Task Invoke(HttpContext context, ICorrelationIdGenerator correlationIdGenerator)
        {
            var correlationId = GetCorrelationId(context, correlationIdGenerator);
            AddCorrelationIdHeaderToResponse(context, correlationId);

            await _next(context);
        }

        private static StringValues GetCorrelationId(HttpContext context, ICorrelationIdGenerator correlationIdGenerator)
        {
            if (context.Request.Headers.TryGetValue(_correlationIdHeader, out var correlationId))
            {
                if (!StringValues.IsNullOrEmpty(correlationId))
                {
                    correlationIdGenerator.Set(correlationId.ToString());
                }

                return correlationId;
            }
            else
            {
                correlationId = Guid.NewGuid().ToString();
                correlationIdGenerator.Set(correlationId.ToString());
                return correlationId;
            }
        }

        private static void AddCorrelationIdHeaderToResponse(HttpContext context, StringValues correlationId)
       => context.Response.OnStarting(() =>
       {
           context.Response.Headers[_correlationIdHeader] = new[] { correlationId.ToString() };
           return Task.CompletedTask;
       });
    }

    public static class CorrelationMiddlewareExtensions
    {
        public static IApplicationBuilder UseCorrelationMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CorrelationMiddleware>();
        }
    }
}