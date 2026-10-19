namespace EcommerceOrderProcessing.Middleware
{
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;
        private const string CorrelationIdHeaderName = "X-Correlation-ID";

        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 1. Check if a Correlation ID was passed from a gateway/client, otherwise create one
            if (!context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
                context.Request.Headers[CorrelationIdHeaderName] = correlationId;
            }

            // 2. Map it to ASP.NET Core's TraceIdentifier for built-in tracing tracking
            context.TraceIdentifier = correlationId;

            // 3. Ensure the response returns the Correlation ID back to the client
            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey(CorrelationIdHeaderName))
                {
                    context.Response.Headers.Append(CorrelationIdHeaderName, correlationId);
                }
                return Task.CompletedTask;
            });

            // 4. Create a Built-in Logging Scope 
            // Any ILogger.Log() called inside this block will automatically include "CorrelationId"
            using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId.ToString() }))
            {
                await _next(context);
            }
        }
    }
}
