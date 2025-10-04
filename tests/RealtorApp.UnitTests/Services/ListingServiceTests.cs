using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RealtorApp.Domain.Models;
using RealtorApp.Domain.Services;
using RealtorApp.UnitTests.Helpers;

namespace RealtorApp.UnitTests.Services;

public class ListingServiceTests : IDisposable
{
    private readonly RealtorAppDbContext _dbContext;
    private readonly ListingService _listingService;
    private TestDataManager _testData;

    public ListingServiceTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        var connectionString = configuration.GetConnectionString("Default");

        var options = new DbContextOptionsBuilder<RealtorAppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        _dbContext = new RealtorAppDbContext(options);

        CleanupAllTestData();

        _testData = new TestDataManager(_dbContext);
        _listingService = new ListingService(_dbContext);
    }

    private void CleanupAllTestData()
    {
        _dbContext.Database.ExecuteSqlRaw(@"
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

    [Fact]
    public async System.Threading.Tasks.Task GetListingDetailsSlim_WithValidListingId_ReturnsListingDetails()
    {
        var listingId = await SetupTestData_SingleListingWithClients();

        var result = await _listingService.GetListingDetailsSlim(listingId);

        Assert.NotNull(result);
        Assert.Equal("123 Main Street", result.Address);
        Assert.Equal(2, result.ClientNames.Length);
        Assert.Contains("Client One", result.ClientNames);
        Assert.Contains("Client Two", result.ClientNames);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetListingDetailsSlim_WithSingleClient_ReturnsOneClientName()
    {
        var listingId = await SetupTestData_ListingWithSingleClient();

        var result = await _listingService.GetListingDetailsSlim(listingId);

        Assert.NotNull(result);
        Assert.Single(result.ClientNames);
        Assert.Contains("Client One", result.ClientNames);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetListingDetailsSlim_WithNoOtherListings_ReturnsEmptyOtherListings()
    {
        var listingId = await SetupTestData_ListingWithNoOtherListings();

        var result = await _listingService.GetListingDetailsSlim(listingId);

        Assert.NotNull(result);
        Assert.Empty(result.OtherListings);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetListingDetailsSlim_WithOtherListings_ReturnsOtherListings()
    {
        var listingId = await SetupTestData_ListingWithOtherListings();

        var result = await _listingService.GetListingDetailsSlim(listingId);

        Assert.NotNull(result);
        Assert.NotEmpty(result.OtherListings);
        Assert.Equal(2, result.OtherListings.Length);
        Assert.Contains(result.OtherListings, ol => ol.Address == "456 Oak Avenue");
        Assert.Contains(result.OtherListings, ol => ol.Address == "789 Pine Street");
    }

    [Fact]
    public async System.Threading.Tasks.Task GetListingDetailsSlim_ExcludesCurrentListingFromOtherListings()
    {
        var listingId = await SetupTestData_ListingWithOtherListings();

        var result = await _listingService.GetListingDetailsSlim(listingId);

        Assert.NotNull(result);
        Assert.DoesNotContain(result.OtherListings, ol => ol.ListingId == listingId);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetListingDetailsSlim_WithSharedClients_ReturnsDistinctOtherListings()
    {
        var listingId = await SetupTestData_ListingWithSharedClients();

        var result = await _listingService.GetListingDetailsSlim(listingId);

        Assert.NotNull(result);
        var distinctListings = result.OtherListings.Select(ol => ol.ListingId).Distinct().Count();
        Assert.Equal(result.OtherListings.Length, distinctListings);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetListingDetailsSlim_WithInvalidListingId_ThrowsException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _listingService.GetListingDetailsSlim(99999);
        });
    }

    #region Test Data Setup

    private async System.Threading.Tasks.Task<long> SetupTestData_SingleListingWithClients()
    {
        var agentUser = _testData.CreateUser("agent@test.com", "Agent", "One");
        var client1User = _testData.CreateUser("client1@test.com", "Client", "One");
        var client2User = _testData.CreateUser("client2@test.com", "Client", "Two");

        var agent = _testData.CreateAgent(agentUser);
        var client1 = _testData.CreateClient(client1User);
        var client2 = _testData.CreateClient(client2User);

        var property = _testData.CreateProperty("123 Main Street");
        var listing = _testData.CreateListing(property.PropertyId);

        _testData.CreateAgentListing(listing.ListingId, agent.UserId);
        _testData.CreateClientListing(listing.ListingId, client1.UserId);
        _testData.CreateClientListing(listing.ListingId, client2.UserId);

        return await System.Threading.Tasks.Task.FromResult(listing.ListingId);
    }

    private async System.Threading.Tasks.Task<long> SetupTestData_ListingWithSingleClient()
    {
        var agentUser = _testData.CreateUser("agent@test.com", "Agent", "One");
        var client1User = _testData.CreateUser("client1@test.com", "Client", "One");

        var agent = _testData.CreateAgent(agentUser);
        var client1 = _testData.CreateClient(client1User);

        var property = _testData.CreateProperty("456 Elm Street");
        var listing = _testData.CreateListing(property.PropertyId);

        _testData.CreateAgentListing(listing.ListingId, agent.UserId);
        _testData.CreateClientListing(listing.ListingId, client1.UserId);

        return await System.Threading.Tasks.Task.FromResult(listing.ListingId);
    }

    private async System.Threading.Tasks.Task<long> SetupTestData_ListingWithNoOtherListings()
    {
        var agentUser = _testData.CreateUser("agent@test.com", "Agent", "One");
        var client1User = _testData.CreateUser("client1@test.com", "Client", "One");

        var agent = _testData.CreateAgent(agentUser);
        var client1 = _testData.CreateClient(client1User);

        var property = _testData.CreateProperty("789 Cedar Lane");
        var listing = _testData.CreateListing(property.PropertyId);

        _testData.CreateAgentListing(listing.ListingId, agent.UserId);
        _testData.CreateClientListing(listing.ListingId, client1.UserId);

        return await System.Threading.Tasks.Task.FromResult(listing.ListingId);
    }

    private async System.Threading.Tasks.Task<long> SetupTestData_ListingWithOtherListings()
    {
        var agentUser = _testData.CreateUser("agent@test.com", "Agent", "One");
        var client1User = _testData.CreateUser("client1@test.com", "Client", "One");

        var agent = _testData.CreateAgent(agentUser);
        var client1 = _testData.CreateClient(client1User);

        var property1 = _testData.CreateProperty("111 First Street");
        var property2 = _testData.CreateProperty("456 Oak Avenue");
        var property3 = _testData.CreateProperty("789 Pine Street");

        var listing1 = _testData.CreateListing(property1.PropertyId);
        var listing2 = _testData.CreateListing(property2.PropertyId);
        var listing3 = _testData.CreateListing(property3.PropertyId);

        _testData.CreateAgentListing(listing1.ListingId, agent.UserId);
        _testData.CreateClientListing(listing1.ListingId, client1.UserId);

        _testData.CreateAgentListing(listing2.ListingId, agent.UserId);
        _testData.CreateClientListing(listing2.ListingId, client1.UserId);

        _testData.CreateAgentListing(listing3.ListingId, agent.UserId);
        _testData.CreateClientListing(listing3.ListingId, client1.UserId);

        return await System.Threading.Tasks.Task.FromResult(listing1.ListingId);
    }

    private async System.Threading.Tasks.Task<long> SetupTestData_ListingWithSharedClients()
    {
        var agentUser = _testData.CreateUser("agent@test.com", "Agent", "One");
        var client1User = _testData.CreateUser("client1@test.com", "Client", "One");
        var client2User = _testData.CreateUser("client2@test.com", "Client", "Two");

        var agent = _testData.CreateAgent(agentUser);
        var client1 = _testData.CreateClient(client1User);
        var client2 = _testData.CreateClient(client2User);

        var property1 = _testData.CreateProperty("100 Shared Street");
        var property2 = _testData.CreateProperty("200 Common Avenue");
        var property3 = _testData.CreateProperty("300 Joint Road");

        var listing1 = _testData.CreateListing(property1.PropertyId);
        var listing2 = _testData.CreateListing(property2.PropertyId);
        var listing3 = _testData.CreateListing(property3.PropertyId);

        _testData.CreateAgentListing(listing1.ListingId, agent.UserId);
        _testData.CreateClientListing(listing1.ListingId, client1.UserId);
        _testData.CreateClientListing(listing1.ListingId, client2.UserId);

        _testData.CreateAgentListing(listing2.ListingId, agent.UserId);
        _testData.CreateClientListing(listing2.ListingId, client1.UserId);

        _testData.CreateAgentListing(listing3.ListingId, agent.UserId);
        _testData.CreateClientListing(listing3.ListingId, client2.UserId);

        return await System.Threading.Tasks.Task.FromResult(listing1.ListingId);
    }

    #endregion

    public void Dispose()
    {
        _testData.Dispose();
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
