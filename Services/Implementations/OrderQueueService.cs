using Amazon.SQS;
using Amazon.SQS.Model;
using EcommerceOrderProcessing.Services.Interfaces;
using System.Text.Json;

namespace EcommerceOrderProcessing.Services.Implementations
{
    /// <summary>
    /// Service responsible for enqueuing orders into the AWS SQS order queue.
    /// </summary>
    public class OrderQueueService : IOrderQueueService
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly string _queueUrl;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderQueueService"/> class.
        /// </summary>
        /// <param name="sqsClient">The Amazon SQS client used to interact with the SQS service.</param>
        /// <param name="configuration">The application configuration containing the SQS queue URL.</param>
        /// <exception cref="InvalidOperationException"></exception>
        public OrderQueueService(IAmazonSQS sqsClient, IConfiguration configuration)
        {
            _sqsClient = sqsClient;
            _queueUrl = configuration["AWSQueueSettings:OrderQueueUrl"]
                        ?? throw new InvalidOperationException("Queue URL not configured.");
        }
        /// <summary>
        /// Enqueues a pending order into the AWS SQS order queue with an optional delay.
        /// </summary>
        /// <param name="orderId">The ID of the order to enqueue.</param>
        /// <param name="delaySeconds">The delay in seconds before the message becomes visible in the queue. Default is 300 seconds (5 minutes).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task EnqueuePendingOrderAsync(int orderId, int delaySeconds = 300)
        {
            var messageBody = JsonSerializer.Serialize(new { OrderId = orderId });

            var request = new SendMessageRequest
            {
                QueueUrl = _queueUrl,
                MessageBody = messageBody,
                DelaySeconds = delaySeconds // Delays message visibility for 5 minutes (max 900 seconds)
            };

            await _sqsClient.SendMessageAsync(request);
        }
    }
}
