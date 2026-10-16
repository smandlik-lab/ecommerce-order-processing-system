using Moq;
using EcommerceOrderProcessing.Repositories.Interfaces;
using EcommerceOrderProcessing.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EcommerceOrderProcessing.Tests.Fixtures
{
    public abstract class BaseServiceTest
    {
        protected readonly Mock<IUserRepository> MockUserRepository;
        protected readonly Mock<IOrderRepository> MockOrderRepository;
        protected readonly Mock<IUnitOfWork> MockUnitOfWork;
        protected readonly Mock<IConfiguration> MockConfiguration;

        protected BaseServiceTest()
        {
            MockUserRepository = new Mock<IUserRepository>();
            MockOrderRepository = new Mock<IOrderRepository>();
            MockUnitOfWork = new Mock<IUnitOfWork>();
            MockConfiguration = new Mock<IConfiguration>();
        }

        protected Mock<ILogger<T>> CreateMockLogger<T>() where T : class
        {
            return new Mock<ILogger<T>>();
        }
    }

    public abstract class BaseControllerTest
    {
        protected readonly MockRepository MockRepository;

        protected BaseControllerTest()
        {
            MockRepository = new MockRepository(MockBehavior.Strict);
        }

        protected Mock<T> CreateMock<T>() where T : class
        {
            return new Mock<T>();
        }
    }
}
