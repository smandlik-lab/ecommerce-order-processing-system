namespace EcommerceOrderProcessing.DTOs.OrderDTOs
{
    /// <summary>
    /// Data transfer object representing an order message received from the SQS order queue.
    /// </summary>
    public class OrderMessageDto
    {
        /// <summary>
        /// Gets or sets the ID of the order.
        /// </summary>
        public int OrderId { get; set; }
    }
}
