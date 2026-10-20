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
            var correlationId = context.Request.Headers[CorrelationIdHeaderName].FirstOrDefault();
            if (string.IsNullOrEmpty(correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
            }
            context.Request.Headers[CorrelationIdHeaderName] = correlationId;

            context.TraceIdentifier = correlationId;

            if (!context.Response.Headers.ContainsKey(CorrelationIdHeaderName))
            {
                context.Response.Headers.Append(CorrelationIdHeaderName, correlationId);
            }

            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey(CorrelationIdHeaderName))
                {
                    context.Response.Headers.Append(CorrelationIdHeaderName, correlationId);
                }
                return Task.CompletedTask;
            });

            var activity = System.Diagnostics.Activity.Current;
            var parentSpanId = activity?.ParentSpanId.ToString() ?? string.Empty;
            var traceId = activity?.TraceId.ToString() ?? string.Empty;
            var spanId = activity?.SpanId.ToString() ?? string.Empty;

            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId.ToString(),
                ["TransId"] = traceId,
                ["TraceId"] = traceId,
                ["SpanId"] = spanId,
                ["ParentId"] = parentSpanId,
                ["ConnectionId"] = context.Connection.Id ?? string.Empty
            }))
            {
                await _next(context);
            }
        }
    }
}
