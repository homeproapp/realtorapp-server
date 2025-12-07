using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RealtorApp.Domain.DTOs;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Infra.Data;
using RealtorApp.Domain.Services;
using RealtorApp.Domain.Settings;
using RealtorApp.UnitTests.Helpers;

namespace RealtorApp.UnitTests.Services;

public abstract class TestBase : IDisposable
{
    protected readonly RealtorAppDbContext DbContext;
    protected readonly Mock<IEmailService> MockEmailService;
    protected readonly Mock<IUserService> MockUserService;
    protected readonly Mock<IAuthProviderService> MockAuthProviderService;
    protected readonly Mock<ICryptoService> MockCryptoService;
    protected readonly Mock<IJwtService> MockJwtService;
    protected readonly Mock<AppSettings> MockAppsettings;
    protected readonly Mock<IRefreshTokenService> MockRefreshTokenService;
    protected readonly Mock<ISqlQueryService> MockSqlQueryService;
    protected readonly Mock<ILogger<InvitationService>> MockLogger;
    protected readonly InvitationService InvitationService;
    protected readonly TestDataManager TestDataManager;

    protected TestBase()
    {
        // Setup PostgreSQL database using appsettings.json
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        var connectionString = configuration.GetConnectionString("Default");

        var options = new DbContextOptionsBuilder<RealtorAppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        DbContext = new RealtorAppDbContext(options);

        // Clear all test data before each test
        CleanupAllTestData();

        // Setup mocks
        MockEmailService = new Mock<IEmailService>();
        MockUserService = new Mock<IUserService>();
        MockAppsettings = new Mock<AppSettings>();
        MockAuthProviderService = new Mock<IAuthProviderService>();
        MockCryptoService = new Mock<ICryptoService>();
        MockJwtService = new Mock<IJwtService>();
        MockRefreshTokenService = new Mock<IRefreshTokenService>();
        MockSqlQueryService = new Mock<ISqlQueryService>();
        MockLogger = new Mock<ILogger<InvitationService>>();

        // Initialize test data manager
        TestDataManager = new TestDataManager(DbContext);

        // Create service under test
        InvitationService = new InvitationService(
            DbContext,
            MockEmailService.Object,
            MockUserService.Object,
            MockAuthProviderService.Object,
            MockCryptoService.Object,
            MockJwtService.Object,
            MockRefreshTokenService.Object,
            MockLogger.Object);

        // Setup default mock behaviors
        SetupDefaultMocks();
    }

    private void SetupDefaultMocks()
    {
        MockUserService.Setup(x => x.GetAgentName(It.IsAny<long>()))
            .ReturnsAsync("Test Agent");

        MockCryptoService.Setup(x => x.Encrypt(It.IsAny<string>()))
            .Returns<string>(data => $"encrypted_{data}");

        MockEmailService.Setup(x => x.SendBulkInvitationEmailsAsync(It.IsAny<List<InvitationEmailDto>>()))
            .ReturnsAsync(new List<InvitationEmailDto>());

        MockAuthProviderService.Setup(x => x.ValidateTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(new AuthProviderUserDto { Uid = Guid.NewGuid().ToString(), Email = "test@example.com" });

        MockJwtService.Setup(x => x.GenerateAccessToken(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test_access_token");

        MockRefreshTokenService.Setup(x => x.CreateRefreshTokenAsync(It.IsAny<long>()))
            .ReturnsAsync("test_refresh_token");
    }

    protected Agent CreateTestAgent(long? userId = null)
    {
        var userEmail = $"agent{Guid.NewGuid():N}@example.com";
        var user = TestDataManager.CreateUser(userEmail, "Test", "Agent");
        return TestDataManager.CreateAgent(user);
    }

    protected Client CreateTestClient(long? userId = null, string? uuid = null)
    {
        var userEmail = $"client{Guid.NewGuid():N}@example.com";
        var user = TestDataManager.CreateUser(userEmail, "Test", "Client", uuid);
        return TestDataManager.CreateClient(user);
    }

    protected ClientInvitation CreateTestClientInvitation(long agentUserId, DateTime? expiresAt = null)
    {
        return TestDataManager.CreateClientInvitation(agentUserId, null, "John", "Doe", "+1234567890");
    }

    protected Property CreateTestProperty(string addressLine1, string city, string region, string postalCode, string countryCode)
    {
        return TestDataManager.CreateProperty(addressLine1, city, region, postalCode, countryCode);
    }

    protected PropertyInvitation CreateTestPropertyInvitation(string addressLine1, string city, string region, string postalCode, string countryCode, long invitedBy)
    {
        return TestDataManager.CreatePropertyInvitation(addressLine1, city, region, postalCode, countryCode, invitedBy);
    }

    private void CleanupAllTestData()
    {
        DbContext.Database.ExecuteSqlRaw(@"
            DELETE FROM contact_attachments;
            DELETE FROM task_attachments;
            DELETE FROM attachments;
            DELETE FROM files_tasks;
            DELETE FROM message_reads;
            DELETE FROM messages;
            DELETE FROM notifications;
            DELETE FROM tasks;
            DELETE FROM files;
            DELETE FROM links;
            DELETE FROM third_party_contacts;
            DELETE FROM client_invitations_properties;
            DELETE FROM clients_listings;
            DELETE FROM agents_listings;
            DELETE FROM property_invitations;
            DELETE FROM client_invitations;
            DELETE FROM conversations;
            DELETE FROM listings;
            DELETE FROM properties;
            DELETE FROM refresh_tokens;
            DELETE FROM clients;
            DELETE FROM agents;
            DELETE FROM users;
            DELETE FROM task_titles;
            DELETE FROM file_types;
        ");
    }

    public virtual void Dispose()
    {
        TestDataManager?.Dispose();
        DbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
