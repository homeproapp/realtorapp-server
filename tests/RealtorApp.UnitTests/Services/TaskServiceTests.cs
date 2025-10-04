using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RealtorApp.Contracts.Queries.Tasks.Requests;
using RealtorApp.Domain.Models;
using RealtorApp.Domain.Services;
using RealtorApp.UnitTests.Helpers;
using TaskStatus = RealtorApp.Contracts.Enums.TaskStatus;

namespace RealtorApp.UnitTests.Services;

public class TaskServiceTests : IDisposable
{
    private readonly RealtorAppDbContext _dbContext;
    private readonly TaskService _taskService;
    private TestDataManager _testData;

    public TaskServiceTests()
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
        _taskService = new TaskService(_dbContext);
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
    public async System.Threading.Tasks.Task GetClientGroupedTasksListAsync_WithMultipleClientsOnSameListing_GroupsThemTogether()
    {
        var agentId = await SetupTestData_MultipleClientsOneListing();

        var result = await _taskService.GetClientGroupedTasksListAsync(new ClientGroupedTasksListQuery(), agentId);

        Assert.NotNull(result);
        Assert.Single(result.ClientGroupedTasksDetails);

        var group = result.ClientGroupedTasksDetails[0];
        Assert.Equal(2, group.Clients.Length);
        Assert.Contains(group.Clients, c => c.FirstName == "Client" && c.LastName == "One");
        Assert.Contains(group.Clients, c => c.FirstName == "Client" && c.LastName == "Two");
    }

    [Fact]
    public async System.Threading.Tasks.Task GetClientGroupedTasksListAsync_WithSameClientsDifferentListings_GroupsThemTogether()
    {
        var agentId = await SetupTestData_SameClientsDifferentListings();

        var result = await _taskService.GetClientGroupedTasksListAsync(new ClientGroupedTasksListQuery(), agentId);

        Assert.NotNull(result);
        Assert.Single(result.ClientGroupedTasksDetails);

        var group = result.ClientGroupedTasksDetails[0];
        Assert.Equal(2, group.TotalListings);
        Assert.Equal(2, group.Clients.Length);
        Assert.Contains(group.Clients, c => c.FirstName == "Client" && c.LastName == "One");
        Assert.Contains(group.Clients, c => c.FirstName == "Client" && c.LastName == "Two");
    }

    [Fact]
    public async System.Threading.Tasks.Task GetClientGroupedTasksListAsync_WithDifferentClientGroups_ReturnsMultipleGroups()
    {
        var agentId = await SetupTestData_DifferentClientGroups();

        var result = await _taskService.GetClientGroupedTasksListAsync(new ClientGroupedTasksListQuery(), agentId);

        Assert.NotNull(result);
        Assert.Equal(2, result.ClientGroupedTasksDetails.Count);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetClientGroupedTasksListAsync_WithTaskStatuses_CountsThemCorrectly()
    {
        var agentId = await SetupTestData_WithVariousTaskStatuses();

        var result = await _taskService.GetClientGroupedTasksListAsync(new ClientGroupedTasksListQuery(), agentId);

        Assert.NotNull(result);
        Assert.Single(result.ClientGroupedTasksDetails);

        var group = result.ClientGroupedTasksDetails[0];
        Assert.Equal(2, group.TaskStatusCounts[TaskStatus.NotStarted]);
        Assert.Equal(1, group.TaskStatusCounts[TaskStatus.InProgress]);
        Assert.Equal(1, group.TaskStatusCounts[TaskStatus.Completed]);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetClientGroupedTasksListAsync_WithNoTasks_ReturnsEmptyStatusCounts()
    {
        var agentId = await SetupTestData_ListingWithNoTasks();

        var result = await _taskService.GetClientGroupedTasksListAsync(new ClientGroupedTasksListQuery(), agentId);

        Assert.NotNull(result);
        Assert.Single(result.ClientGroupedTasksDetails);

        var group = result.ClientGroupedTasksDetails[0];
        Assert.Empty(group.TaskStatusCounts);
        Assert.True(group.ClickThroughListingId > 0);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetClientGroupedTasksListAsync_WithMostRecentTaskUpdate_SetsCorrectClickThrough()
    {
        var agentId = await SetupTestData_MultipleListingsWithTasks();

        var result = await _taskService.GetClientGroupedTasksListAsync(new ClientGroupedTasksListQuery(), agentId);

        Assert.NotNull(result);
        Assert.Single(result.ClientGroupedTasksDetails);

        var group = result.ClientGroupedTasksDetails[0];
        Assert.True(group.ClickThroughListingId > 0);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetClientGroupedTasksListAsync_WithNullTaskStatuses_SkipsThem()
    {
        var agentId = await SetupTestData_WithNullStatusTasks();

        var result = await _taskService.GetClientGroupedTasksListAsync(new ClientGroupedTasksListQuery(), agentId);

        Assert.NotNull(result);
        Assert.Single(result.ClientGroupedTasksDetails);

        var group = result.ClientGroupedTasksDetails[0];
        Assert.Equal(2, group.TaskStatusCounts[TaskStatus.InProgress]);
        Assert.False(group.TaskStatusCounts.ContainsKey((TaskStatus)0));
    }

    [Fact]
    public async System.Threading.Tasks.Task GetClientGroupedTasksListAsync_WithMultipleListingsSameClients_CountsListingsCorrectly()
    {
        var agentId = await SetupTestData_MultipleListingsSameClients();

        var result = await _taskService.GetClientGroupedTasksListAsync(new ClientGroupedTasksListQuery(), agentId);

        Assert.NotNull(result);
        Assert.Single(result.ClientGroupedTasksDetails);

        var group = result.ClientGroupedTasksDetails[0];
        Assert.Equal(3, group.TotalListings);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetClientGroupedTasksListAsync_WithNoListings_ReturnsEmptyList()
    {
        var agentUser = _testData.CreateUser("agent@test.com", "Agent", "One");
        var agent = _testData.CreateAgent(agentUser);

        var result = await _taskService.GetClientGroupedTasksListAsync(new ClientGroupedTasksListQuery(), agent.UserId);

        Assert.NotNull(result);
        Assert.Empty(result.ClientGroupedTasksDetails);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetClientGroupedTasksListAsync_OrdersClientIdsByDescending_ForGrouping()
    {
        var agentId = await SetupTestData_ClientsInDifferentOrder();

        var result = await _taskService.GetClientGroupedTasksListAsync(new ClientGroupedTasksListQuery(), agentId);

        Assert.NotNull(result);
        Assert.Single(result.ClientGroupedTasksDetails);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetClientGroupedTasksListAsync_WithMixedTasksAcrossListings_AggregatesStatusCounts()
    {
        var agentId = await SetupTestData_MixedTasksAcrossListings();

        var result = await _taskService.GetClientGroupedTasksListAsync(new ClientGroupedTasksListQuery(), agentId);

        Assert.NotNull(result);
        Assert.Single(result.ClientGroupedTasksDetails);

        var group = result.ClientGroupedTasksDetails[0];
        Assert.Equal(3, group.TaskStatusCounts[TaskStatus.NotStarted]);
        Assert.Equal(2, group.TaskStatusCounts[TaskStatus.Completed]);
        Assert.Equal(2, group.TotalListings);
    }

    #region Test Data Setup

    private async System.Threading.Tasks.Task<long> SetupTestData_MultipleClientsOneListing()
    {
        var agentUser = _testData.CreateUser("agent@test.com", "Agent", "One");
        var client1User = _testData.CreateUser("client1@test.com", "Client", "One");
        var client2User = _testData.CreateUser("client2@test.com", "Client", "Two");

        var agent = _testData.CreateAgent(agentUser);
        var client1 = _testData.CreateClient(client1User);
        var client2 = _testData.CreateClient(client2User);

        var property = _testData.CreateProperty();
        var listing = _testData.CreateListing(property.PropertyId);

        _testData.CreateAgentListing(listing.ListingId, agent.UserId);
        _testData.CreateClientListing(listing.ListingId, client1.UserId);
        _testData.CreateClientListing(listing.ListingId, client2.UserId);

        _testData.CreateTask(listing.ListingId, "Task 1", (short)TaskStatus.NotStarted);

        return await System.Threading.Tasks.Task.FromResult(agent.UserId);
    }

    private async System.Threading.Tasks.Task<long> SetupTestData_SameClientsDifferentListings()
    {
        var agentUser = _testData.CreateUser("agent@test.com", "Agent", "One");
        var client1User = _testData.CreateUser("client1@test.com", "Client", "One");
        var client2User = _testData.CreateUser("client2@test.com", "Client", "Two");

        var agent = _testData.CreateAgent(agentUser);
        var client1 = _testData.CreateClient(client1User);
        var client2 = _testData.CreateClient(client2User);

        var property1 = _testData.CreateProperty();
        var property2 = _testData.CreateProperty();
        var listing1 = _testData.CreateListing(property1.PropertyId);
        var listing2 = _testData.CreateListing(property2.PropertyId);

        _testData.CreateAgentListing(listing1.ListingId, agent.UserId);
        _testData.CreateClientListing(listing1.ListingId, client1.UserId);
        _testData.CreateClientListing(listing1.ListingId, client2.UserId);

        _testData.CreateAgentListing(listing2.ListingId, agent.UserId);
        _testData.CreateClientListing(listing2.ListingId, client1.UserId);
        _testData.CreateClientListing(listing2.ListingId, client2.UserId);

        _testData.CreateTask(listing1.ListingId, "Task 1", (short)TaskStatus.NotStarted);
        _testData.CreateTask(listing2.ListingId, "Task 2", (short)TaskStatus.InProgress);

        return await System.Threading.Tasks.Task.FromResult(agent.UserId);
    }

    private async System.Threading.Tasks.Task<long> SetupTestData_DifferentClientGroups()
    {
        var agentUser = _testData.CreateUser("agent@test.com", "Agent", "One");
        var client1User = _testData.CreateUser("client1@test.com", "Client", "One");
        var client2User = _testData.CreateUser("client2@test.com", "Client", "Two");
        var client3User = _testData.CreateUser("client3@test.com", "Client", "Three");

        var agent = _testData.CreateAgent(agentUser);
        var client1 = _testData.CreateClient(client1User);
        var client2 = _testData.CreateClient(client2User);
        var client3 = _testData.CreateClient(client3User);

        var property1 = _testData.CreateProperty();
        var property2 = _testData.CreateProperty();
        var listing1 = _testData.CreateListing(property1.PropertyId);
        var listing2 = _testData.CreateListing(property2.PropertyId);

        _testData.CreateAgentListing(listing1.ListingId, agent.UserId);
        _testData.CreateClientListing(listing1.ListingId, client1.UserId);
        _testData.CreateClientListing(listing1.ListingId, client2.UserId);

        _testData.CreateAgentListing(listing2.ListingId, agent.UserId);
        _testData.CreateClientListing(listing2.ListingId, client3.UserId);

        _testData.CreateTask(listing1.ListingId, "Task 1", (short)TaskStatus.NotStarted);
        _testData.CreateTask(listing2.ListingId, "Task 2", (short)TaskStatus.InProgress);

        return await System.Threading.Tasks.Task.FromResult(agent.UserId);
    }

    private async System.Threading.Tasks.Task<long> SetupTestData_WithVariousTaskStatuses()
    {
        var agentUser = _testData.CreateUser("agent@test.com", "Agent", "One");
        var client1User = _testData.CreateUser("client1@test.com", "Client", "One");

        var agent = _testData.CreateAgent(agentUser);
        var client1 = _testData.CreateClient(client1User);

        var property = _testData.CreateProperty();
        var listing = _testData.CreateListing(property.PropertyId);

        _testData.CreateAgentListing(listing.ListingId, agent.UserId);
        _testData.CreateClientListing(listing.ListingId, client1.UserId);

        _testData.CreateTask(listing.ListingId, "Task 1", (short)TaskStatus.NotStarted);
        _testData.CreateTask(listing.ListingId, "Task 2", (short)TaskStatus.NotStarted);
        _testData.CreateTask(listing.ListingId, "Task 3", (short)TaskStatus.InProgress);
        _testData.CreateTask(listing.ListingId, "Task 4", (short)TaskStatus.Completed);

        return await System.Threading.Tasks.Task.FromResult(agent.UserId);
    }

    private async System.Threading.Tasks.Task<long> SetupTestData_ListingWithNoTasks()
    {
        var agentUser = _testData.CreateUser("agent@test.com", "Agent", "One");
        var client1User = _testData.CreateUser("client1@test.com", "Client", "One");

        var agent = _testData.CreateAgent(agentUser);
        var client1 = _testData.CreateClient(client1User);

        var property = _testData.CreateProperty();
        var listing = _testData.CreateListing(property.PropertyId);

        _testData.CreateAgentListing(listing.ListingId, agent.UserId);
        _testData.CreateClientListing(listing.ListingId, client1.UserId);

        return await System.Threading.Tasks.Task.FromResult(agent.UserId);
    }

    private async System.Threading.Tasks.Task<long> SetupTestData_MultipleListingsWithTasks()
    {
        var agentUser = _testData.CreateUser("agent@test.com", "Agent", "One");
        var client1User = _testData.CreateUser("client1@test.com", "Client", "One");

        var agent = _testData.CreateAgent(agentUser);
        var client1 = _testData.CreateClient(client1User);

        var property1 = _testData.CreateProperty();
        var property2 = _testData.CreateProperty();
        var listing1 = _testData.CreateListing(property1.PropertyId);
        var listing2 = _testData.CreateListing(property2.PropertyId);

        _testData.CreateAgentListing(listing1.ListingId, agent.UserId);
        _testData.CreateClientListing(listing1.ListingId, client1.UserId);

        _testData.CreateAgentListing(listing2.ListingId, agent.UserId);
        _testData.CreateClientListing(listing2.ListingId, client1.UserId);

        _testData.CreateTask(listing1.ListingId, "Task 1", (short)TaskStatus.NotStarted, DateTime.UtcNow.AddMinutes(-10));
        _testData.CreateTask(listing2.ListingId, "Task 2", (short)TaskStatus.InProgress, DateTime.UtcNow.AddMinutes(-1));

        return await System.Threading.Tasks.Task.FromResult(agent.UserId);
    }

    private async System.Threading.Tasks.Task<long> SetupTestData_WithNullStatusTasks()
    {
        var agentUser = _testData.CreateUser("agent@test.com", "Agent", "One");
        var client1User = _testData.CreateUser("client1@test.com", "Client", "One");

        var agent = _testData.CreateAgent(agentUser);
        var client1 = _testData.CreateClient(client1User);

        var property = _testData.CreateProperty();
        var listing = _testData.CreateListing(property.PropertyId);

        _testData.CreateAgentListing(listing.ListingId, agent.UserId);
        _testData.CreateClientListing(listing.ListingId, client1.UserId);

        _testData.CreateTask(listing.ListingId, "Task 1", null);
        _testData.CreateTask(listing.ListingId, "Task 2", (short)TaskStatus.InProgress);
        _testData.CreateTask(listing.ListingId, "Task 3", (short)TaskStatus.InProgress);

        return await System.Threading.Tasks.Task.FromResult(agent.UserId);
    }

    private async System.Threading.Tasks.Task<long> SetupTestData_MultipleListingsSameClients()
    {
        var agentUser = _testData.CreateUser("agent@test.com", "Agent", "One");
        var client1User = _testData.CreateUser("client1@test.com", "Client", "One");

        var agent = _testData.CreateAgent(agentUser);
        var client1 = _testData.CreateClient(client1User);

        var property1 = _testData.CreateProperty();
        var property2 = _testData.CreateProperty();
        var property3 = _testData.CreateProperty();
        var listing1 = _testData.CreateListing(property1.PropertyId);
        var listing2 = _testData.CreateListing(property2.PropertyId);
        var listing3 = _testData.CreateListing(property3.PropertyId);

        _testData.CreateAgentListing(listing1.ListingId, agent.UserId);
        _testData.CreateClientListing(listing1.ListingId, client1.UserId);

        _testData.CreateAgentListing(listing2.ListingId, agent.UserId);
        _testData.CreateClientListing(listing2.ListingId, client1.UserId);

        _testData.CreateAgentListing(listing3.ListingId, agent.UserId);
        _testData.CreateClientListing(listing3.ListingId, client1.UserId);

        _testData.CreateTask(listing1.ListingId, "Task 1", (short)TaskStatus.NotStarted);
        _testData.CreateTask(listing2.ListingId, "Task 2", (short)TaskStatus.InProgress);
        _testData.CreateTask(listing3.ListingId, "Task 3", (short)TaskStatus.Completed);

        return await System.Threading.Tasks.Task.FromResult(agent.UserId);
    }

    private async System.Threading.Tasks.Task<long> SetupTestData_ClientsInDifferentOrder()
    {
        var agentUser = _testData.CreateUser("agent@test.com", "Agent", "One");
        var client1User = _testData.CreateUser("client1@test.com", "Client", "One");
        var client2User = _testData.CreateUser("client2@test.com", "Client", "Two");

        var agent = _testData.CreateAgent(agentUser);
        var client1 = _testData.CreateClient(client1User);
        var client2 = _testData.CreateClient(client2User);

        var property1 = _testData.CreateProperty();
        var property2 = _testData.CreateProperty();
        var listing1 = _testData.CreateListing(property1.PropertyId);
        var listing2 = _testData.CreateListing(property2.PropertyId);

        _testData.CreateAgentListing(listing1.ListingId, agent.UserId);
        _testData.CreateClientListing(listing1.ListingId, client1.UserId);
        _testData.CreateClientListing(listing1.ListingId, client2.UserId);

        _testData.CreateAgentListing(listing2.ListingId, agent.UserId);
        _testData.CreateClientListing(listing2.ListingId, client2.UserId);
        _testData.CreateClientListing(listing2.ListingId, client1.UserId);

        _testData.CreateTask(listing1.ListingId, "Task 1", (short)TaskStatus.NotStarted);
        _testData.CreateTask(listing2.ListingId, "Task 2", (short)TaskStatus.InProgress);

        return await System.Threading.Tasks.Task.FromResult(agent.UserId);
    }

    private async System.Threading.Tasks.Task<long> SetupTestData_MixedTasksAcrossListings()
    {
        var agentUser = _testData.CreateUser("agent@test.com", "Agent", "One");
        var client1User = _testData.CreateUser("client1@test.com", "Client", "One");

        var agent = _testData.CreateAgent(agentUser);
        var client1 = _testData.CreateClient(client1User);

        var property1 = _testData.CreateProperty();
        var property2 = _testData.CreateProperty();
        var listing1 = _testData.CreateListing(property1.PropertyId);
        var listing2 = _testData.CreateListing(property2.PropertyId);

        _testData.CreateAgentListing(listing1.ListingId, agent.UserId);
        _testData.CreateClientListing(listing1.ListingId, client1.UserId);

        _testData.CreateAgentListing(listing2.ListingId, agent.UserId);
        _testData.CreateClientListing(listing2.ListingId, client1.UserId);

        _testData.CreateTask(listing1.ListingId, "Task 1", (short)TaskStatus.NotStarted);
        _testData.CreateTask(listing1.ListingId, "Task 2", (short)TaskStatus.NotStarted);
        _testData.CreateTask(listing1.ListingId, "Task 3", (short)TaskStatus.Completed);

        _testData.CreateTask(listing2.ListingId, "Task 4", (short)TaskStatus.NotStarted);
        _testData.CreateTask(listing2.ListingId, "Task 5", (short)TaskStatus.Completed);

        return await System.Threading.Tasks.Task.FromResult(agent.UserId);
    }

    #endregion

    public void Dispose()
    {
        _testData.Dispose();
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
