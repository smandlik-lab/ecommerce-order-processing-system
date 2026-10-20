using Moq;
using Xunit;
using FluentAssertions;
using Amazon.SQS;
using Amazon.SQS.Model;
using EcommerceOrderProcessing.Services.Implementations;
using EcommerceOrderProcessing.Tests.Fixtures;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace EcommerceOrderProcessing.Tests.Services
{
    public class OrderQueueServiceTests : BaseServiceTest
    {
        private readonly OrderQueueService _orderQueueService;
        private const string TestQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789/test-queue";

        public OrderQueueServiceTests()
        {
            SetupConfigurationMock("AWSQueueSettings:OrderQueueUrl", TestQueueUrl);

            _orderQueueService = new OrderQueueService(
                MockSqsClient.Object,
                MockConfiguration.Object
            );
        }

        #region EnqueuePendingOrderAsync Tests

        [Fact]
        public async Task EnqueuePendingOrderAsync_WithValidOrderId_ShouldSendMessageToSqs()
        {
            // Arrange
            int orderId = 1;
            SendMessageResponse sendResponse = new SendMessageResponse()
            {
                MessageId = Guid.NewGuid().ToString()
            };

            MockSqsClient.Setup(s => s.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sendResponse);

            // Act
            await _orderQueueService.EnqueuePendingOrderAsync(orderId);

            // Assert
            MockSqsClient.Verify(
                s => s.SendMessageAsync(
                    It.Is<SendMessageRequest>(req =>
                        req.QueueUrl == TestQueueUrl &&
                        req.DelaySeconds == 300),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task EnqueuePendingOrderAsync_WithCustomDelay_ShouldUseProvidedDelaySeconds()
        {
            // Arrange
            int orderId = 1;
            int customDelay = 600; // 10 minutes
            SendMessageResponse sendResponse = new SendMessageResponse()
            {
                MessageId = Guid.NewGuid().ToString()
            };

            MockSqsClient.Setup(s => s.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sendResponse);

            // Act
            await _orderQueueService.EnqueuePendingOrderAsync(orderId, customDelay);

            // Assert
            MockSqsClient.Verify(
                s => s.SendMessageAsync(
                    It.Is<SendMessageRequest>(req =>
                        req.DelaySeconds == customDelay),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task EnqueuePendingOrderAsync_ShouldSerializeOrderIdInMessageBody()
        {
            // Arrange
            int orderId = 42;
            SendMessageResponse sendResponse = new SendMessageResponse()
            {
                MessageId = Guid.NewGuid().ToString()
            };

            MockSqsClient.Setup(s => s.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sendResponse);

            // Act
            await _orderQueueService.EnqueuePendingOrderAsync(orderId);

            // Assert
            MockSqsClient.Verify(
                s => s.SendMessageAsync(
                    It.Is<SendMessageRequest>(req =>
                        req.MessageBody.Contains(orderId.ToString())),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task EnqueuePendingOrderAsync_ShouldContainValidJsonInMessageBody()
        {
            // Arrange
            int orderId = 1;
            SendMessageResponse sendResponse = new SendMessageResponse()
            {
                MessageId = Guid.NewGuid().ToString()
            };

            string capturedMessageBody = null!;
            MockSqsClient.Setup(s => s.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
                .Callback<SendMessageRequest, CancellationToken>((req, ct) => capturedMessageBody = req.MessageBody)
                .ReturnsAsync(sendResponse);

            // Act
            await _orderQueueService.EnqueuePendingOrderAsync(orderId);

            // Assert
            capturedMessageBody.Should().NotBeNullOrEmpty();
            var jsonDoc = JsonDocument.Parse(capturedMessageBody);
            jsonDoc.RootElement.GetProperty("OrderId").GetInt32().Should().Be(orderId);
        }

        [Fact]
        public async Task EnqueuePendingOrderAsync_WithMissingQueueUrl_ShouldThrowInvalidOperationException()
        {
            // Arrange
            SetupConfigurationMock("AWSQueueSettings:OrderQueueUrl", null!);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                new OrderQueueService(MockSqsClient.Object, MockConfiguration.Object));
            ex.Message.Should().Contain("Queue URL not configured");
        }

        [Fact]
        public async Task EnqueuePendingOrderAsync_WithDelayZero_ShouldSendImmediate()
        {
            // Arrange
            int orderId = 1;
            int delaySeconds = 0;
            SendMessageResponse sendResponse = new SendMessageResponse()
            {
                MessageId = Guid.NewGuid().ToString()
            };

            MockSqsClient.Setup(s => s.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sendResponse);

            // Act
            await _orderQueueService.EnqueuePendingOrderAsync(orderId, delaySeconds);

            // Assert
            MockSqsClient.Verify(
                s => s.SendMessageAsync(
                    It.Is<SendMessageRequest>(req =>
                        req.DelaySeconds == 0),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task EnqueuePendingOrderAsync_WithMaxDelay_ShouldSendWithMaxDelaySeconds()
        {
            // Arrange
            int orderId = 1;
            int maxDelay = 900; // SQS max delay is 900 seconds (15 minutes)
            SendMessageResponse sendResponse = new SendMessageResponse()
            {
                MessageId = Guid.NewGuid().ToString()
            };

            MockSqsClient.Setup(s => s.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sendResponse);

            // Act
            await _orderQueueService.EnqueuePendingOrderAsync(orderId, maxDelay);

            // Assert
            MockSqsClient.Verify(
                s => s.SendMessageAsync(
                    It.Is<SendMessageRequest>(req =>
                        req.DelaySeconds == maxDelay),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task EnqueuePendingOrderAsync_ShouldUseCorrectQueueUrl()
        {
            // Arrange
            int orderId = 1;
            SendMessageResponse sendResponse = new SendMessageResponse()
            {
                MessageId = Guid.NewGuid().ToString()
            };

            MockSqsClient.Setup(s => s.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sendResponse);

            // Act
            await _orderQueueService.EnqueuePendingOrderAsync(orderId);

            // Assert
            MockSqsClient.Verify(
                s => s.SendMessageAsync(
                    It.Is<SendMessageRequest>(req =>
                        req.QueueUrl == TestQueueUrl),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task EnqueuePendingOrderAsync_ShouldHandleSqsException()
        {
            // Arrange
            int orderId = 1;
            MockSqsClient.Setup(s => s.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new AmazonSQSException("Service unavailable"));

            // Act & Assert
            await Assert.ThrowsAsync<AmazonSQSException>(() => _orderQueueService.EnqueuePendingOrderAsync(orderId));
        }

        #endregion
    }
}
