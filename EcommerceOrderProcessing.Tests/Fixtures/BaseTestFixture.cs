using Moq;
using EcommerceOrderProcessing.Repositories.Interfaces;
using EcommerceOrderProcessing.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Amazon.SQS;
using Microsoft.AspNetCore.Http;

namespace EcommerceOrderProcessing.Tests.Fixtures
{
    public abstract class BaseServiceTest
    {
        protected readonly Mock<IUserRepository> MockUserRepository;
        protected readonly Mock<IOrderRepository> MockOrderRepository;
        protected readonly Mock<IUnitOfWork> MockUnitOfWork;
        protected readonly Mock<IConfiguration> MockConfiguration;
        protected readonly Mock<IAmazonSQS> MockSqsClient;
        protected readonly Mock<IHttpContextAccessor> MockHttpContextAccessor;

        protected BaseServiceTest()
        {
            MockUserRepository = new Mock<IUserRepository>();
            MockOrderRepository = new Mock<IOrderRepository>();
            MockUnitOfWork = new Mock<IUnitOfWork>();
            MockConfiguration = new Mock<IConfiguration>();
            MockSqsClient = new Mock<IAmazonSQS>();
            MockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        }

        protected Mock<ILogger<T>> CreateMockLogger<T>() where T : class
        {
            return new Mock<ILogger<T>>();
        }

        protected void SetupConfigurationMock(string key, string value)
        {
            MockConfiguration.Setup(c => c[key]).Returns(value);
        }

        protected void SetupHttpContextMock(string correlationId = null)
        {
            var httpContext = new DefaultHttpContext();
            if (!string.IsNullOrEmpty(correlationId))
            {
                httpContext.TraceIdentifier = correlationId;
            }
            MockHttpContextAccessor.Setup(h => h.HttpContext).Returns(httpContext);
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
