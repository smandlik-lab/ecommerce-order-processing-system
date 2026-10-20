using Microsoft.AspNetCore.Http;

namespace EcommerceOrderProcessing.Middleware
{
    public class CorrelationIdDelegatingHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string CorrelationIdHeaderName = "X-Correlation-ID";

        public CorrelationIdDelegatingHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Pull the active Correlation ID from the current HttpContext request
            var correlationId = _httpContextAccessor.HttpContext?.TraceIdentifier;

            if (!string.IsNullOrEmpty(correlationId) && !request.Headers.Contains(CorrelationIdHeaderName))
            {
                request.Headers.Add(CorrelationIdHeaderName, correlationId);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
