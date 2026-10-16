using EcommerceOrderProcessing.DTOs;
using EcommerceOrderProcessing.DTOs.Auth;
using EcommerceOrderProcessing.DTOs.OrderDTOs;
using EcommerceOrderProcessing.Models;

namespace EcommerceOrderProcessing.Tests.Fixtures
{
    public static class TestDataFactory
    {
        public static User CreateTestUser(int id = 1, string email = "test@example.com")
        {
            return new User
            {
                Id = id,
                FullName = "Test User",
                Email = email,
                PasswordHash = "hashedpassword123",
                PhoneNumber = "1234567890",
                Role = UserRole.Customer
            };
        }

        public static RegisterDto CreateRegisterDto(string email = "newuser@example.com")
        {
            return new RegisterDto
            {
                FullName = "New User",
                Email = email,
                Password = "Password123!",
                PhoneNumber = "9876543210"
            };
        }

        public static LoginDto CreateLoginDto(string email = "test@example.com", string password = "Password123!")
        {
            return new LoginDto
            {
                Email = email,
                Password = password
            };
        }

        public static Order CreateTestOrder(int id = 1, int userId = 1, OrderStatus status = OrderStatus.Pending)
        {
            return new Order
            {
                Id = id,
                UserId = userId,
                TotalAmount = 99.99m,
                Status = status,
                CreatedAt = DateTime.UtcNow,
                OrderItems = new List<OrderItem>
                {
                    new OrderItem
                    {
                        Id = 1,
                        OrderId = id,
                        ProductId = 1,
                        Quantity = 2,
                        UnitPrice = 49.99m
                    }
                }
            };
        }

        public static Product CreateTestProduct(int id = 1, string name = "Test Product")
        {
            return new Product
            {
                Id = id,
                Name = name,
                Price = 49.99m,
                StockQuantity = 100,
                CategoryId = 1,
                Description = "Test product description"
            };
        }

        public static Cart CreateTestCart(int userId = 1)
        {
            return new Cart
            {
                Id = 1,
                UserId = userId,
                CartItems = new List<CartItem>
                {
                    new CartItem
                    {
                        Id = 1,
                        CartId = 1,
                        ProductId = 1,
                        Quantity = 2,
                        Product = CreateTestProduct()
                    }
                }
            };
        }

        public static UpdateOrderStatusDto CreateUpdateOrderStatusDto(OrderStatus status = OrderStatus.Completed)
        {
            return new UpdateOrderStatusDto
            {
                Status = nameof(OrderStatus.Completed)
            };
        }

        public static UpdatePaymentStatusDto CreateUpdatePaymentStatusDto()
        {
            return new UpdatePaymentStatusDto
            {
                PaymentStatus = "Completed"
            };
        }

        public static UpdateProfileDto CreateUpdateProfileDto(string fullName = "Updated Name")
        {
            return new UpdateProfileDto
            {
                FullName = fullName,
                PhoneNumber = "1111111111"
            };
        }

        public static ChangePasswordDto CreateChangePasswordDto()
        {
            return new ChangePasswordDto
            {
                CurrentPassword = "OldPassword123!",
                NewPassword = "NewPassword123!",
                ConfirmNewPassword = "NewPassword123!"
            };
        }

        public static ChangeEmailDto CreateChangeEmailDto(string newEmail = "newemail@example.com")
        {
            return new ChangeEmailDto
            {
                NewEmail = newEmail,
                Password = "Password123!"
            };
        }
    }
}
