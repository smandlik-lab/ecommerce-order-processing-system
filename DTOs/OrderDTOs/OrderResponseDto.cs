namespace EcommerceOrderProcessing.DTOs.OrderDTOs
{
    public class OrderResponseDto
    {
        public int OrderId { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<OrderItemResponseDto> Items { get; set; } = new();
    }
}
