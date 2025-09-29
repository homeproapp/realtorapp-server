using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RealtorApp.Domain.DTOs;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Domain.Models;
using RealtorApp.Domain.Services;

namespace RealtorApp.UnitTests.Services;

public abstract class TestBase : IDisposable
{
    protected readonly RealtorAppDbContext DbContext;
    protected readonly Mock<IEmailService> MockEmailService;
    protected readonly Mock<IUserService> MockUserService;
    protected readonly Mock<IAuthProviderService> MockAuthProviderService;
    protected readonly Mock<ICryptoService> MockCryptoService;
    protected readonly Mock<IJwtService> MockJwtService;
    protected readonly Mock<IRefreshTokenService> MockRefreshTokenService;
    protected readonly InvitationService InvitationService;

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

        // Setup mocks
        MockEmailService = new Mock<IEmailService>();
        MockUserService = new Mock<IUserService>();
        MockAuthProviderService = new Mock<IAuthProviderService>();
        MockCryptoService = new Mock<ICryptoService>();
        MockJwtService = new Mock<IJwtService>();
        MockRefreshTokenService = new Mock<IRefreshTokenService>();

        // Create service under test
        InvitationService = new InvitationService(
            DbContext,
            MockEmailService.Object,
            MockUserService.Object,
            MockAuthProviderService.Object,
            MockCryptoService.Object,
            MockJwtService.Object,
            MockRefreshTokenService.Object);

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

        MockJwtService.Setup(x => x.GenerateAccessToken(It.IsAny<Guid>(), It.IsAny<string>()))
            .Returns("test_access_token");

        MockRefreshTokenService.Setup(x => x.CreateRefreshTokenAsync(It.IsAny<long>()))
            .ReturnsAsync("test_refresh_token");
    }

    protected Agent CreateTestAgent(long userId = 1)
    {
        var user = new User
        {
            UserId = userId,
            Uuid = Guid.NewGuid(),
            Email = "agent@example.com",
            FirstName = "Test",
            LastName = "Agent",
            CreatedAt = DateTime.UtcNow
        };

        var agent = new Agent
        {
            UserId = userId,
            User = user,
            CreatedAt = DateTime.UtcNow
        };

        DbContext.Users.Add(user);
        DbContext.Agents.Add(agent);
        DbContext.SaveChanges();

        return agent;
    }

    protected Client CreateTestClient(long userId = 2, string? uuid = null)
    {
        var user = new User
        {
            UserId = userId,
            Uuid = uuid != null ? Guid.Parse(uuid) : Guid.NewGuid(),
            Email = "client@example.com",
            FirstName = "Test",
            LastName = "Client",
            CreatedAt = DateTime.UtcNow
        };

        var client = new Client
        {
            UserId = userId,
            User = user,
            CreatedAt = DateTime.UtcNow
        };

        DbContext.Users.Add(user);
        DbContext.Clients.Add(client);
        DbContext.SaveChanges();

        return client;
    }

    protected ClientInvitation CreateTestClientInvitation(long agentUserId = 1, DateTime? expiresAt = null)
    {
        var invitation = new ClientInvitation
        {
            ClientEmail = "test@example.com",
            ClientFirstName = "John",
            ClientLastName = "Doe",
            ClientPhone = "+1234567890",
            InvitationToken = Guid.NewGuid(),
            InvitedBy = agentUserId,
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        DbContext.ClientInvitations.Add(invitation);
        DbContext.SaveChanges();

        return invitation;
    }

    public void Dispose()
    {
        DbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}