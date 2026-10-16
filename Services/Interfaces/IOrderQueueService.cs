namespace EcommerceOrderProcessing.Services.Interfaces
{
    /// <summary>
    /// Interface defining the contract for the order queue service responsible for enqueuing orders into the AWS SQS order queue.
    /// </summary>
    public interface IOrderQueueService
    {
        /// <summary>
        /// Enqueues a pending order into the AWS SQS order queue with an optional delay.
        /// </summary>
        /// <param name="orderId">The ID of the order to enqueue.</param>
        /// <param name="delaySeconds">The delay in seconds before the message becomes visible in the queue. Default is 300 seconds (5 minutes).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task EnqueuePendingOrderAsync(int orderId, int delaySeconds = 300);
    }
}
