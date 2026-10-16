using System.ComponentModel.DataAnnotations;

namespace EcommerceOrderProcessing.Models
{
    public class Order
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public int UserId { get; set; }
        public User? User { get; set; }
        public string? CustomerEmail { get; set; }

        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        //public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
        //public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        [MaxLength(100)]
        public string RecipientName { get; set; } = string.Empty;

        //public Governorate Governorate { get; set; }

        [MaxLength(300)]
        public string AddressLine { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Notes { get; set; }

        public decimal SubTotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal TotalAmount { get; set; }

        public string? StripeSessionId { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
