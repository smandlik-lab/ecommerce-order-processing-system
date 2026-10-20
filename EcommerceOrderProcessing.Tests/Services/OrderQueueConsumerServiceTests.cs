using Moq;
using Xunit;
using FluentAssertions;
using Amazon.SQS;
using EcommerceOrderProcessing.Services.BackgroundServices;
using EcommerceOrderProcessing.Tests.Fixtures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EcommerceOrderProcessing.Tests.Services
{
    public class OrderQueueConsumerServiceTests : BaseServiceTest
    {
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<ILogger<OrderQueueConsumerService>> _mockLogger;
        private const string TestQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789/test-queue";

        public OrderQueueConsumerServiceTests()
        {
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockLogger = CreateMockLogger<OrderQueueConsumerService>();

            SetupConfigurationMock("AWSQueueSettings:OrderQueueUrl", TestQueueUrl);
        }

        private OrderQueueConsumerService CreateConsumerService()
        {
            return new OrderQueueConsumerService(
                _mockServiceProvider.Object,
                MockSqsClient.Object,
                MockConfiguration.Object,
                _mockLogger.Object
            );
        }

        #region Construction Tests

        [Fact]
        public void Constructor_WithValidConfig_ShouldInitializeSuccessfully()
        {
            // Act
            var service = CreateConsumerService();

            // Assert
            service.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithMissingQueueUrl_ShouldThrowInvalidOperationException()
        {
            // Arrange
            SetupConfigurationMock("AWSQueueSettings:OrderQueueUrl", null!);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                new OrderQueueConsumerService(_mockServiceProvider.Object, MockSqsClient.Object, MockConfiguration.Object, _mockLogger.Object));
            ex.Message.Should().Contain("Queue URL not configured");
        }

        #endregion

        #region Dependency Injection Tests

        [Fact]
        public void Constructor_ShouldAcceptServiceProvider()
        {
            // Arrange & Act
            var service = CreateConsumerService();

            // Assert
            service.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_ShouldAcceptSqsClient()
        {
            // Arrange & Act
            var service = CreateConsumerService();

            // Assert
            service.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_ShouldAcceptConfiguration()
        {
            // Arrange & Act
            var service = CreateConsumerService();

            // Assert
            service.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_ShouldAcceptLogger()
        {
            // Arrange & Act
            var service = CreateConsumerService();

            // Assert
            service.Should().NotBeNull();
        }

        #endregion

        #region Configuration Validation Tests

        [Fact]
        public void Constructor_ShouldValidateQueueUrlConfiguration()
        {
            // Arrange
            SetupConfigurationMock("AWSQueueSettings:OrderQueueUrl", TestQueueUrl);

            // Act & Assert
            var ex = Record.Exception(() => CreateConsumerService());
            ex.Should().BeNull();
        }

        [Fact]
        public void Constructor_WithEmptyQueueUrl_ShouldThrowException()
        {
            // Arrange
            SetupConfigurationMock("AWSQueueSettings:OrderQueueUrl", string.Empty);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                CreateConsumerService());
            ex.Message.Should().Contain("Queue URL not configured");
        }

        #endregion

        #region BackgroundService Lifecycle Tests

        [Fact]
        public async Task StartAsync_ShouldCompleteWithoutError()
        {
            // Arrange
            var service = CreateConsumerService();

            // Act
            await service.StartAsync(CancellationToken.None);

            // Assert
            service.Should().NotBeNull();
        }

        [Fact]
        public async Task StopAsync_ShouldCompleteWithoutError()
        {
            // Arrange
            var service = CreateConsumerService();
            await service.StartAsync(CancellationToken.None);

            // Act
            await service.StopAsync(CancellationToken.None);

            // Assert
            service.Should().NotBeNull();
        }

        #endregion
    }
}
