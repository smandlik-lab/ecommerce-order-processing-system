using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using EcommerceOrderProcessing.DTOs.OrderDTOs;
using EcommerceOrderProcessing.Data;
using EcommerceOrderProcessing.Models;
using Microsoft.Extensions.Configuration;

namespace EcommerceOrderProcessing.Services.BackgroundServices
{
    /// <summary>
    /// Background service responsible for consuming and processing orders from the AWS SQS order queue.
    /// </summary>
    public class OrderQueueConsumerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IAmazonSQS _sqsClient;
        private readonly string _queueUrl;
        private readonly ILogger<OrderQueueConsumerService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderQueueConsumerService"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider used to create scopes for resolving scoped services.</param>
        /// <param name="sqsClient">The Amazon SQS client used to interact with the SQS service.</param>
        /// <param name="configuration">The application configuration containing the SQS queue URL.</param>
        /// <param name="logger">The logger used for logging information and errors.</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <remarks>
        /// This exception is thrown if the SQS queue URL is not configured in the application settings.
        /// </remarks>
        public OrderQueueConsumerService(
            IServiceProvider serviceProvider,
            IAmazonSQS sqsClient,
            IConfiguration configuration,
            ILogger<OrderQueueConsumerService> logger)
        {
            _serviceProvider = serviceProvider;
            _sqsClient = sqsClient;
            _queueUrl = configuration["AWSQueueSettings:OrderQueueUrl"]
                        ?? throw new InvalidOperationException("Queue URL not configured.");
            _logger = logger;
        }

        /// <summary>
        /// Executes the background service, continuously polling the SQS order queue for new messages and processing them.
        /// </summary>
        /// <param name="stoppingToken">The cancellation token used to stop the background service.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Order SQS Consumer Worker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var receiveRequest = new ReceiveMessageRequest
                    {
                        QueueUrl = _queueUrl,
                        MaxNumberOfMessages = 10,
                        WaitTimeSeconds = 20 // Enable long polling (reduces empty requests and cost)
                    };

                    var response = await _sqsClient.ReceiveMessageAsync(receiveRequest, stoppingToken);

                    if (response.Messages != null && response.Messages.Count > 0)
                    {
                        foreach (var message in response.Messages)
                        {
                            await ProcessOrderMessageAsync(message, stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while receiving messages from SQS.");
                    await Task.Delay(5000, stoppingToken); // Back off briefly on error
                }
            }
        }

        /// <summary>
        /// Processes a single order message received from the SQS order queue.
        /// </summary>
        /// <param name="message">The SQS message representing the order.</param>
        /// <param name="stoppingToken">The cancellation token used to stop the background service.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ProcessOrderMessageAsync(Message message, CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            try
            {
                _logger.LogInformation("Processing SQS message: {MessageId}", message.MessageId);
                var orderData = JsonSerializer.Deserialize<OrderMessageDto>(message.Body);
                if (orderData != null)
                {
                    _logger.LogInformation("Deserialized order data. OrderId: {OrderId}", orderData.OrderId);
                    var order = await dbContext.Orders.FindAsync(new object[] { orderData.OrderId }, stoppingToken);

                    // Only update if it's still PENDING (in case it was manually processed or cancelled earlier)
                    if (order != null && order.Status == OrderStatus.Pending)
                    {
                        order.Status = OrderStatus.Completed;
                        await dbContext.SaveChangesAsync(stoppingToken);

                        _logger.LogInformation("Order {OrderId} status successfully updated to COMPLETED.", orderData.OrderId);
                    }
                    else if (order == null)
                    {
                        _logger.LogWarning("Order not found in database. OrderId: {OrderId}", orderData.OrderId);
                    }
                    else
                    {
                        _logger.LogInformation("Order {OrderId} is not in PENDING status. Current status: {CurrentStatus}", orderData.OrderId, order.Status);
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to deserialize order data from message: {MessageId}", message.MessageId);
                }

                // Acknowledge and delete message from queue upon successful processing
                await _sqsClient.DeleteMessageAsync(new DeleteMessageRequest
                {
                    QueueUrl = _queueUrl,
                    ReceiptHandle = message.ReceiptHandle
                }, stoppingToken);
                _logger.LogInformation("Message deleted from queue successfully. MessageId: {MessageId}", message.MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process SQS message ID: {MessageId}. Details: {ErrorMessage}", message.MessageId, ex.Message);
                // Note: If deletion fails, SQS will automatically make the message visible again after the Visibility Timeout expires.
            }
        }
    }
}
