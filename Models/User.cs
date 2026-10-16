namespace EcommerceOrderProcessing.Models
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Customer;

        public List<Order> Orders { get; set; } = new();
        public Cart? Cart { get; set; }
    }
}
    