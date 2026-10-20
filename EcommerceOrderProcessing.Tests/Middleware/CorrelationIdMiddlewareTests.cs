using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using EcommerceOrderProcessing.Middleware;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Diagnostics;

namespace EcommerceOrderProcessing.Tests.Middleware
{
    public class CorrelationIdMiddlewareTests
    {
        private readonly Mock<ILogger<CorrelationIdMiddleware>> _mockLogger;
        private const string CorrelationIdHeaderName = "X-Correlation-ID";

        public CorrelationIdMiddlewareTests()
        {
            _mockLogger = new Mock<ILogger<CorrelationIdMiddleware>>();
        }

        private CorrelationIdMiddleware CreateMiddleware(RequestDelegate next = null!)
        {
            return new CorrelationIdMiddleware(next ?? (ctx => Task.CompletedTask), _mockLogger.Object);
        }

        #region Header Handling Tests

        [Fact]
        public async Task InvokeAsync_WithExistingCorrelationIdHeader_ShouldPreserveIt()
        {
            // Arrange
            var providedCorrelationId = Guid.NewGuid().ToString();
            var middleware = CreateMiddleware();
            var context = new DefaultHttpContext();
            context.Request.Headers[CorrelationIdHeaderName] = providedCorrelationId;

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.TraceIdentifier.Should().Be(providedCorrelationId);
        }

        [Fact]
        public async Task InvokeAsync_WithoutCorrelationIdHeader_ShouldGenerateNew()
        {
            // Arrange
            var middleware = CreateMiddleware();
            var context = new DefaultHttpContext();

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.TraceIdentifier.Should().NotBeNullOrEmpty();
            Guid.TryParse(context.TraceIdentifier, out var result).Should().BeTrue();
            context.Request.Headers[CorrelationIdHeaderName].Should().NotBeEmpty();
        }

        [Fact]
        public async Task InvokeAsync_ShouldAddCorrelationIdToRequestHeader()
        {
            // Arrange
            var middleware = CreateMiddleware();
            var context = new DefaultHttpContext();

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Request.Headers.Should().ContainKey(CorrelationIdHeaderName);
        }

        [Fact]
        public async Task InvokeAsync_ShouldAddCorrelationIdToResponseHeader()
        {
            // Arrange
            var middleware = CreateMiddleware();
            var context = new DefaultHttpContext();

            // Act
            await middleware.InvokeAsync(context);
            await context.Response.StartAsync();

            // Assert
            context.Response.Headers.Should().ContainKey(CorrelationIdHeaderName);
            context.Response.Headers[CorrelationIdHeaderName].Should().NotBeEmpty();
        }

        [Fact]
        public async Task InvokeAsync_ShouldNotOverwriteExistingResponseCorrelationId()
        {
            // Arrange
            var existingCorrelationId = Guid.NewGuid().ToString();
            var middleware = CreateMiddleware();
            var context = new DefaultHttpContext();
            context.Request.Headers[CorrelationIdHeaderName] = existingCorrelationId;

            // Act
            await middleware.InvokeAsync(context);
            await context.Response.StartAsync();

            // Assert
            context.Response.Headers[CorrelationIdHeaderName].ToString().Should().Be(existingCorrelationId);
        }

        #endregion

        #region TraceIdentifier Mapping Tests

        [Fact]
        public async Task InvokeAsync_ShouldMapCorrelationIdToTraceIdentifier()
        {
            // Arrange
            var providedCorrelationId = Guid.NewGuid().ToString();
            var middleware = CreateMiddleware();
            var context = new DefaultHttpContext();
            context.Request.Headers[CorrelationIdHeaderName] = providedCorrelationId;

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.TraceIdentifier.Should().Be(providedCorrelationId);
        }

        [Fact]
        public async Task InvokeAsync_ShouldMapGeneratedCorrelationIdToTraceIdentifier()
        {
            // Arrange
            var middleware = CreateMiddleware();
            var context = new DefaultHttpContext();

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Guid.TryParse(context.TraceIdentifier, out _).Should().BeTrue();
            context.TraceIdentifier.Should().Be(context.Request.Headers[CorrelationIdHeaderName]);
        }

        #endregion

        #region Logging Scope Tests

        [Fact]
        public async Task InvokeAsync_ShouldCreateLoggingScope()
        {
            // Arrange
            var middleware = CreateMiddleware();
            var context = new DefaultHttpContext();

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            _mockLogger.Verify(
                x => x.BeginScope(It.IsAny<Dictionary<string, object>>()),
                Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_ShouldIncludeCorrelationIdInLoggingScope()
        {
            // Arrange
            var providedCorrelationId = Guid.NewGuid().ToString();
            var middleware = CreateMiddleware();
            var context = new DefaultHttpContext();
            context.Request.Headers[CorrelationIdHeaderName] = providedCorrelationId;

            Dictionary<string, object>? capturedScope = null;

            _mockLogger.Setup(
                x => x.BeginScope(It.IsAny<Dictionary<string, object>>()))
                .Callback<Dictionary<string, object>>(scope => capturedScope = scope)
                .Returns(new Mock<IDisposable>().Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            capturedScope.Should().NotBeNull();
            var scope = capturedScope!;
            scope.Should().ContainKey("CorrelationId");
            scope["CorrelationId"].Should().Be(providedCorrelationId);
        }

        [Fact]
        public async Task InvokeAsync_ShouldIncludeDistributedTraceIdentifiersInLoggingScope()
        {
            var traceId = "0123456789abcdef0123456789abcdef";
            var parentSpanId = "0123456789abcdef";
            using var activity = new Activity("test-request")
                .SetIdFormat(ActivityIdFormat.W3C)
                .SetParentId($"00-{traceId}-{parentSpanId}-01")
                .Start();
            var context = new DefaultHttpContext();
            Dictionary<string, object>? capturedScope = null;
            _mockLogger.Setup(x => x.BeginScope(It.IsAny<Dictionary<string, object>>()))
                .Callback<Dictionary<string, object>>(scope => capturedScope = scope)
                .Returns(new Mock<IDisposable>().Object);

            await CreateMiddleware().InvokeAsync(context);

            capturedScope.Should().NotBeNull();
            var scope = capturedScope!;
            scope["TransId"].Should().Be(traceId);
            scope["TraceId"].Should().Be(traceId);
            scope["SpanId"].Should().Be(activity.SpanId.ToString());
            scope["ParentId"].Should().Be(parentSpanId);
            scope["ConnectionId"].Should().Be(context.Connection.Id ?? string.Empty);
        }

        #endregion

        #region Middleware Chain Tests

        [Fact]
        public async Task InvokeAsync_ShouldCallNextMiddleware()
        {
            // Arrange
            var nextMiddlewareCalled = false;
            RequestDelegate next = (_) =>
            {
                nextMiddlewareCalled = true;
                return Task.CompletedTask;
            };

            var middleware = new CorrelationIdMiddleware(next, _mockLogger.Object);
            var context = new DefaultHttpContext();

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            nextMiddlewareCalled.Should().BeTrue();
        }

        [Fact]
        public async Task InvokeAsync_ShouldPropagateExceptionsFromNextMiddleware()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Test exception");
            RequestDelegate next = (_) => throw expectedException;

            var middleware = new CorrelationIdMiddleware(next, _mockLogger.Object);
            var context = new DefaultHttpContext();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));
            ex.Should().Be(expectedException);
        }

        #endregion

        #region Edge Cases Tests

        [Fact]
        public async Task InvokeAsync_WithEmptyCorrelationIdHeader_ShouldGenerateNew()
        {
            // Arrange
            var middleware = CreateMiddleware();
            var context = new DefaultHttpContext();
            context.Request.Headers[CorrelationIdHeaderName] = string.Empty;

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.TraceIdentifier.Should().NotBeNullOrEmpty();
            context.TraceIdentifier.Should().NotBe(string.Empty);
        }

        [Fact]
        public async Task InvokeAsync_WithWhitespaceCorrelationIdHeader_ShouldPreserveIt()
        {
            // Arrange
            var whitespaceId = "   ";
            var middleware = CreateMiddleware();
            var context = new DefaultHttpContext();
            context.Request.Headers[CorrelationIdHeaderName] = whitespaceId;

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.TraceIdentifier.Should().Be(whitespaceId);
        }

        [Fact]
        public async Task InvokeAsync_WithLongCorrelationId_ShouldPreserveIt()
        {
            // Arrange
            var longId = string.Concat(Enumerable.Repeat("x", 500));
            var middleware = CreateMiddleware();
            var context = new DefaultHttpContext();
            context.Request.Headers[CorrelationIdHeaderName] = longId;

            // Act
            await middleware.InvokeAsync(context);
            await context.Response.StartAsync();

            // Assert
            context.TraceIdentifier.Should().Be(longId);
            context.Response.Headers[CorrelationIdHeaderName].ToString().Should().Be(longId);
        }

        [Fact]
        public async Task InvokeAsync_WithSpecialCharactersInCorrelationId_ShouldPreserveIt()
        {
            // Arrange
            var specialId = "correlation-id_123!@#$%^&*()";
            var middleware = CreateMiddleware();
            var context = new DefaultHttpContext();
            context.Request.Headers[CorrelationIdHeaderName] = specialId;

            // Act
            await middleware.InvokeAsync(context);
            await context.Response.StartAsync();

            // Assert
            context.TraceIdentifier.Should().Be(specialId);
            context.Response.Headers[CorrelationIdHeaderName].ToString().Should().Be(specialId);
        }

        #endregion
    }
}
