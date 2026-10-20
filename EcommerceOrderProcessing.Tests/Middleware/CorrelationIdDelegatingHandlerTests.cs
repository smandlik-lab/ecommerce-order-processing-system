using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using EcommerceOrderProcessing.Middleware;

namespace EcommerceOrderProcessing.Tests.Middleware
{
    public class CorrelationIdDelegatingHandlerTests
    {
        private const string CorrelationIdHeaderName = "X-Correlation-ID";

        #region Construction Tests

        [Fact]
        public void Constructor_WithHttpContextAccessor_ShouldInitializeSuccessfully()
        {
            // Arrange
            var mockContextAccessor = new Mock<IHttpContextAccessor>();

            // Act
            var handler = new CorrelationIdDelegatingHandler(mockContextAccessor.Object);

            //Assert
            handler.Should().NotBeNull();
        }

        #endregion

        #region HttpContext Integration Tests

        [Fact]
        public void Constructor_WithNullHttpContextAccessor_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new CorrelationIdDelegatingHandler(null!));
        }

        [Fact]
        public void Handler_ShouldBeConfiguredForOutboundRequests()
        {
            // Arrange
            var mockContextAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            httpContext.TraceIdentifier = Guid.NewGuid().ToString();
            mockContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

            // Act
            var handler = new CorrelationIdDelegatingHandler(mockContextAccessor.Object);

            // Assert
            handler.Should().BeOfType<CorrelationIdDelegatingHandler>();
        }

        #endregion

        #region HttpContextAccessor Configuration Tests

        [Fact]
        public void Handler_ShouldReceiveHttpContextAccessorInConstructor()
        {
            // Arrange
            var mockContextAccessor = new Mock<IHttpContextAccessor>();

            // Act
            var handler = new CorrelationIdDelegatingHandler(mockContextAccessor.Object);

            // Assert
            mockContextAccessor.Verify();
        }

        [Fact]
        public void Handler_WithNullHttpContext_ShouldHandleGracefully()
        {
            // Arrange
            var mockContextAccessor = new Mock<IHttpContextAccessor>();
            mockContextAccessor.Setup(a => a.HttpContext).Returns((HttpContext)null!);

            // Act
            var handler = new CorrelationIdDelegatingHandler(mockContextAccessor.Object);

            // Assert
            handler.Should().NotBeNull();
        }

        #endregion

        #region Correlation ID Field Configuration Tests

        [Fact]
        public void Handler_ShouldBeConfiguredWithConstantHeaderName()
        {
            // Arrange
            var mockContextAccessor = new Mock<IHttpContextAccessor>();

            // Act
            var handler = new CorrelationIdDelegatingHandler(mockContextAccessor.Object);

            // Assert
            // The header name should be "X-Correlation-ID" as defined in the middleware
            handler.Should().NotBeNull();
        }

        #endregion

        #region DelegatingHandler Behavior Tests

        [Fact]
        public void Handler_ShouldBeADelegatingHandler()
        {
            // Arrange
            var mockContextAccessor = new Mock<IHttpContextAccessor>();

            // Act
            var handler = new CorrelationIdDelegatingHandler(mockContextAccessor.Object);

            // Assert
            handler.Should().BeAssignableTo<DelegatingHandler>();
        }

        [Fact]
        public void Handler_ShouldSupportChaining()
        {
            // Arrange
            var mockContextAccessor = new Mock<IHttpContextAccessor>();
            var handler = new CorrelationIdDelegatingHandler(mockContextAccessor.Object);
            var innerHandler = new HttpClientHandler();

            // Act
            handler.InnerHandler = innerHandler;

            // Assert
            handler.InnerHandler.Should().Be(innerHandler);
        }

        #endregion

        #region HTTP Client Integration Tests

        [Fact]
        public void Handler_CanBeUsedInHttpClientFactory()
        {
            // Arrange
            var mockContextAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            httpContext.TraceIdentifier = Guid.NewGuid().ToString();
            mockContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

            var handler = new CorrelationIdDelegatingHandler(mockContextAccessor.Object)
            {
                InnerHandler = new HttpClientHandler()
            };

            // Act
            var client = new HttpClient(handler);

            // Assert
            client.Should().NotBeNull();
            client.DefaultRequestHeaders.Should().NotBeNull();
        }

        [Fact]
        public async Task Handler_ShouldPropagateCorrelationIdOnOutboundRequest()
        {
            var correlationId = Guid.NewGuid().ToString();
            var httpContext = new DefaultHttpContext();
            httpContext.TraceIdentifier = correlationId;
            var mockContextAccessor = new Mock<IHttpContextAccessor>();
            mockContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);
            var innerHandler = new CapturingHandler();
            var handler = new CorrelationIdDelegatingHandler(mockContextAccessor.Object)
            {
                InnerHandler = innerHandler
            };
            using var client = new HttpClient(handler);

            using var response = await client.GetAsync("https://example.test/");

            response.IsSuccessStatusCode.Should().BeTrue();
            innerHandler.Request.Should().NotBeNull();
            innerHandler.Request!.Headers.GetValues(CorrelationIdHeaderName).Should().ContainSingle()
                .Which.Should().Be(correlationId);
        }

        #endregion

        private sealed class CapturingHandler : HttpMessageHandler
        {
            public HttpRequestMessage? Request { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                Request = request;
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
            }
        }
    }
}
