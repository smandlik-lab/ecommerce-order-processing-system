namespace EcommerceOrderProcessing.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string? ImageUrl { get; set; }

        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public List<CartItem> CartItems { get; set; } = new();
        public List<OrderItem> OrderItems { get; set; } = new();
    }
}
