using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RealtorApp.Contracts.Commands.Tasks.Requests;
using RealtorApp.Contracts.Enums;
using RealtorApp.Contracts.Queries.Tasks.Requests;
using RealtorApp.Contracts.Queries.Tasks.Responses;
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
            DELETE FROM links;
            DELETE FROM tasks;
            DELETE FROM files;
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

    #region GetListingTasksAsync Tests

    [Fact]
    public async System.Threading.Tasks.Task GetListingTasksAsync_WithTasksInListing_ReturnsAllTasks()
    {
        var listingId = await SetupTestData_ListingWithMultipleTasks();

        var result = await _taskService.GetListingTasksAsync(new ListingTasksQuery(), listingId);

        Assert.NotNull(result);
        Assert.Equal(3, result.Tasks.Length);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetListingTasksAsync_WithTasksWithLinks_ReturnsLinksCorrectly()
    {
        var listingId = await SetupTestData_ListingWithTasksAndLinks();

        var result = await _taskService.GetListingTasksAsync(new ListingTasksQuery(), listingId);

        Assert.NotNull(result);
        Assert.Single(result.Tasks);
        var task = result.Tasks[0];
        Assert.Equal(2, task.Links.Length);
        Assert.Contains(task.Links, l => l.Name == "Link 1");
        Assert.Contains(task.Links, l => l.Name == "Link 2");
    }

    [Fact]
    public async System.Threading.Tasks.Task GetListingTasksAsync_WithNoTasks_ReturnsEmptyArray()
    {
        var agentUser = _testData.CreateUser("agent@test.com", "Agent", "One");
        var agent = _testData.CreateAgent(agentUser);
        var property = _testData.CreateProperty();
        var listing = _testData.CreateListing(property.PropertyId);

        var result = await _taskService.GetListingTasksAsync(new ListingTasksQuery(), listing.ListingId);

        Assert.NotNull(result);
        Assert.Empty(result.Tasks);
        Assert.Empty(result.TaskCompletionCounts);
    }

    private async System.Threading.Tasks.Task<long> SetupTestData_ListingWithMultipleTasks()
    {
        var property = _testData.CreateProperty();
        var listing = _testData.CreateListing(property.PropertyId);

        _testData.CreateTask(listing.ListingId, "Task 1", (short)TaskStatus.NotStarted);
        _testData.CreateTask(listing.ListingId, "Task 2", (short)TaskStatus.InProgress);
        _testData.CreateTask(listing.ListingId, "Task 3", (short)TaskStatus.Completed);

        return await System.Threading.Tasks.Task.FromResult(listing.ListingId);
    }

    private async System.Threading.Tasks.Task<long> SetupTestData_ListingWithTasksAndLinks()
    {
        var property = _testData.CreateProperty();
        var listing = _testData.CreateListing(property.PropertyId);
        var task = _testData.CreateTask(listing.ListingId, "Task with Links", (short)TaskStatus.NotStarted);

        _testData.CreateLink(task.TaskId, "Link 1", "https://example.com/1");
        _testData.CreateLink(task.TaskId, "Link 2", "https://example.com/2");

        return await System.Threading.Tasks.Task.FromResult(listing.ListingId);
    }

    #endregion

    #region GetListingTasksAsync - TaskCompletionCounts Tests

    [Fact]
    public async System.Threading.Tasks.Task GetListingTasksAsync_WithTasksGroupedByRoom_CalculatesCompletionCorrectly()
    {
        var listingId = await SetupTestData_TasksGroupedByRoom();

        var result = await _taskService.GetListingTasksAsync(new ListingTasksQuery(), listingId);

        Assert.NotNull(result);
        Assert.NotEmpty(result.TaskCompletionCounts);

        var kitchenCount = result.TaskCompletionCounts.FirstOrDefault(c => c.Name == "Kitchen" && c.Type == TaskCountType.Room);
        Assert.NotNull(kitchenCount);
        Assert.Equal(0.5, kitchenCount.Completion);

        var bedroomCount = result.TaskCompletionCounts.FirstOrDefault(c => c.Name == "Bedroom" && c.Type == TaskCountType.Room);
        Assert.NotNull(bedroomCount);
        Assert.Equal(1.0, bedroomCount.Completion);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetListingTasksAsync_WithAllTasksCompleted_ReturnsFullCompletion()
    {
        var property = _testData.CreateProperty();
        var listing = _testData.CreateListing(property.PropertyId);

        _testData.CreateTask(listing.ListingId, "Task 1", (short)TaskStatus.Completed);
        _testData.CreateTask(listing.ListingId, "Task 2", (short)TaskStatus.Completed);

        var result = await _taskService.GetListingTasksAsync(new ListingTasksQuery(), listing.ListingId);

        Assert.NotNull(result);
        Assert.All(result.TaskCompletionCounts, count => Assert.Equal(1.0, count.Completion));
    }

    [Fact]
    public async System.Threading.Tasks.Task GetListingTasksAsync_WithNoTasksCompleted_ReturnsZeroCompletion()
    {
        var property = _testData.CreateProperty();
        var listing = _testData.CreateListing(property.PropertyId);

        _testData.CreateTask(listing.ListingId, "Task 1", (short)TaskStatus.NotStarted);
        _testData.CreateTask(listing.ListingId, "Task 2", (short)TaskStatus.InProgress);

        var result = await _taskService.GetListingTasksAsync(new ListingTasksQuery(), listing.ListingId);

        Assert.NotNull(result);
        Assert.All(result.TaskCompletionCounts, count => Assert.Equal(0.0, count.Completion));
    }

    [Fact]
    public async System.Threading.Tasks.Task GetListingTasksAsync_WithTasksGroupedByPriority_CalculatesCompletionCorrectly()
    {
        var listingId = await SetupTestData_TasksGroupedByPriority();

        var result = await _taskService.GetListingTasksAsync(new ListingTasksQuery(), listingId);

        Assert.NotNull(result);
        Assert.NotEmpty(result.TaskCompletionCounts);

        var highPriorityCount = result.TaskCompletionCounts.FirstOrDefault(c => c.Name == "High" && c.Type == TaskCountType.Priority);
        Assert.NotNull(highPriorityCount);
        Assert.Equal(0.5, highPriorityCount.Completion);

        var mediumPriorityCount = result.TaskCompletionCounts.FirstOrDefault(c => c.Name == "Medium" && c.Type == TaskCountType.Priority);
        Assert.NotNull(mediumPriorityCount);
        Assert.Equal(0.0, mediumPriorityCount.Completion);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetListingTasksAsync_WithMixedRoomAndPriority_ReturnsAllCompletionCounts()
    {
        var property = _testData.CreateProperty();
        var listing = _testData.CreateListing(property.PropertyId);

        var task1 = _testData.CreateTask(listing.ListingId, "Task 1", (short)TaskStatus.Completed);
        task1.Room = "Kitchen";
        task1.Priority = (short)TaskPriority.High;

        var task2 = _testData.CreateTask(listing.ListingId, "Task 2", (short)TaskStatus.NotStarted);
        task2.Room = "Bedroom";
        task2.Priority = (short)TaskPriority.Low;

        _dbContext.SaveChanges();

        var result = await _taskService.GetListingTasksAsync(new ListingTasksQuery(), listing.ListingId);

        Assert.NotNull(result);
        Assert.NotEmpty(result.TaskCompletionCounts);

        var roomCounts = result.TaskCompletionCounts.Where(c => c.Type == TaskCountType.Room).ToArray();
        var priorityCounts = result.TaskCompletionCounts.Where(c => c.Type == TaskCountType.Priority).ToArray();

        Assert.Equal(2, roomCounts.Length);
        Assert.Equal(2, priorityCounts.Length);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetListingTasksAsync_WithNullRoomAndPriority_HandlesGracefully()
    {
        var property = _testData.CreateProperty();
        var listing = _testData.CreateListing(property.PropertyId);

        var task1 = _testData.CreateTask(listing.ListingId, "Task 1", (short)TaskStatus.Completed);
        task1.Room = "";
        task1.Priority = 0;

        var task2 = _testData.CreateTask(listing.ListingId, "Task 2", (short)TaskStatus.NotStarted);
        task2.Room = "Kitchen";
        task2.Priority = (short)TaskPriority.High;

        _dbContext.SaveChanges();

        var result = await _taskService.GetListingTasksAsync(new ListingTasksQuery(), listing.ListingId);

        Assert.NotNull(result);
        Assert.NotEmpty(result.TaskCompletionCounts);
    }

    private async System.Threading.Tasks.Task<long> SetupTestData_TasksGroupedByRoom()
    {
        var property = _testData.CreateProperty();
        var listing = _testData.CreateListing(property.PropertyId);

        var task1 = _testData.CreateTask(listing.ListingId, "Kitchen Task 1", (short)TaskStatus.Completed);
        task1.Room = "Kitchen";
        task1.Priority = (short)TaskPriority.High;

        var task2 = _testData.CreateTask(listing.ListingId, "Kitchen Task 2", (short)TaskStatus.NotStarted);
        task2.Room = "Kitchen";
        task2.Priority = (short)TaskPriority.High;

        var task3 = _testData.CreateTask(listing.ListingId, "Bedroom Task 1", (short)TaskStatus.Completed);
        task3.Room = "Bedroom";
        task3.Priority = (short)TaskPriority.Medium;

        _dbContext.SaveChanges();

        return await System.Threading.Tasks.Task.FromResult(listing.ListingId);
    }

    private async System.Threading.Tasks.Task<long> SetupTestData_TasksGroupedByPriority()
    {
        var property = _testData.CreateProperty();
        var listing = _testData.CreateListing(property.PropertyId);

        var task1 = _testData.CreateTask(listing.ListingId, "High Priority Task 1", (short)TaskStatus.Completed);
        task1.Room = "Kitchen";
        task1.Priority = (short)TaskPriority.High;

        var task2 = _testData.CreateTask(listing.ListingId, "High Priority Task 2", (short)TaskStatus.NotStarted);
        task2.Room = "Bedroom";
        task2.Priority = (short)TaskPriority.High;

        var task3 = _testData.CreateTask(listing.ListingId, "Medium Priority Task 1", (short)TaskStatus.NotStarted);
        task3.Room = "Living Room";
        task3.Priority = (short)TaskPriority.Medium;

        _dbContext.SaveChanges();

        return await System.Threading.Tasks.Task.FromResult(listing.ListingId);
    }

    #endregion

    #region AddOrUpdateTaskAsync Tests - Add New Task

    [Fact]
    public async System.Threading.Tasks.Task AddOrUpdateTaskAsync_AddNewTask_CreatesTaskSuccessfully()
    {
        var property = _testData.CreateProperty();
        var listing = _testData.CreateListing(property.PropertyId);

        var command = new AddOrUpdateTaskCommand
        {
            TaskId = null,
            TitleString = "New Task",
            Room = "Living Room",
            Description = "Test description",
            Priority = TaskPriority.High,
            Links = []
        };

        var result = await _taskService.AddOrUpdateTaskAsync(command, listing.ListingId);

        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);
        Assert.True(result.TaskId > 0);

        var createdTask = await _dbContext.Tasks.FindAsync(result.TaskId);
        Assert.NotNull(createdTask);
        Assert.Equal("New Task", createdTask.Title);
        Assert.Equal("Living Room", createdTask.Room);
        Assert.Equal("Test description", createdTask.Description);
        Assert.Equal((short)TaskPriority.High, createdTask.Priority);
        Assert.Equal((short)TaskStatus.NotStarted, createdTask.Status);
    }

    [Fact]
    public async System.Threading.Tasks.Task AddOrUpdateTaskAsync_AddNewTaskWithLinks_CreatesTaskAndLinksSuccessfully()
    {
        var property = _testData.CreateProperty();
        var listing = _testData.CreateListing(property.PropertyId);

        var command = new AddOrUpdateTaskCommand
        {
            TaskId = null,
            TitleString = "New Task with Links",
            Room = "Kitchen",
            Description = "Task with links",
            Priority = TaskPriority.Medium,
            Links = [
                new AddOrUpdateLinkRequest { LinkText = "Google", LinkUrl = "https://google.com" },
                new AddOrUpdateLinkRequest { LinkText = "Example", LinkUrl = "https://example.com" }
            ]
        };

        var result = await _taskService.AddOrUpdateTaskAsync(command, listing.ListingId);

        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);
        Assert.True(result.TaskId > 0);
        Assert.NotNull(result.AddedLinks);
        Assert.Equal(2, result.AddedLinks.Length);
        Assert.Contains(result.AddedLinks, l => l.LinkText == "Google" && l.LinkUrl == "https://google.com");
        Assert.Contains(result.AddedLinks, l => l.LinkText == "Example" && l.LinkUrl == "https://example.com");

        var createdTask = await _dbContext.Tasks
            .Include(t => t.Links)
            .FirstOrDefaultAsync(t => t.TaskId == result.TaskId);
        Assert.NotNull(createdTask);
        Assert.Equal(2, createdTask.Links.Count);
    }

    [Fact]
    public async System.Threading.Tasks.Task AddOrUpdateTaskAsync_AddNewTaskWithEmptyLinks_CreatesTaskWithNoLinks()
    {
        var property = _testData.CreateProperty();
        var listing = _testData.CreateListing(property.PropertyId);

        var command = new AddOrUpdateTaskCommand
        {
            TaskId = null,
            TitleString = "Task without links",
            Room = "Bedroom",
            Priority = TaskPriority.Low,
            Links = []
        };

        var result = await _taskService.AddOrUpdateTaskAsync(command, listing.ListingId);

        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);
        Assert.True(result.TaskId > 0);
        Assert.True(result.AddedLinks == null || result.AddedLinks.Length == 0);
    }

    #endregion

    #region AddOrUpdateTaskAsync Tests - Update Existing Task

    [Fact]
    public async System.Threading.Tasks.Task AddOrUpdateTaskAsync_UpdateExistingTask_UpdatesTaskSuccessfully()
    {
        var property = _testData.CreateProperty();
        var listing = _testData.CreateListing(property.PropertyId);
        var task = _testData.CreateTask(listing.ListingId, "Original Title", (short)TaskStatus.NotStarted);

        var command = new AddOrUpdateTaskCommand
        {
            TaskId = task.TaskId,
            TitleString = "Updated Title",
            Room = "Updated Room",
            Description = "Updated description",
            Priority = TaskPriority.High,
            Links = []
        };

        var result = await _taskService.AddOrUpdateTaskAsync(command, listing.ListingId);

        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(task.TaskId, result.TaskId);

        var updatedTask = await _dbContext.Tasks.FindAsync(task.TaskId);
        Assert.NotNull(updatedTask);
        Assert.Equal("Updated Title", updatedTask.Title);
        Assert.Equal("Updated Room", updatedTask.Room);
        Assert.Equal("Updated description", updatedTask.Description);
        Assert.Equal((short)TaskPriority.High, updatedTask.Priority);
    }

    [Fact]
    public async System.Threading.Tasks.Task AddOrUpdateTaskAsync_UpdateTaskWithNewLinks_AddsLinksSuccessfully()
    {
        var property = _testData.CreateProperty();
        var listing = _testData.CreateListing(property.PropertyId);
        var task = _testData.CreateTask(listing.ListingId, "Task Title", (short)TaskStatus.NotStarted);

        var command = new AddOrUpdateTaskCommand
        {
            TaskId = task.TaskId,
            TitleString = "Task Title",
            Room = "Living Room",
            Priority = TaskPriority.Medium,
            Links = [
                new AddOrUpdateLinkRequest { LinkText = "New Link", LinkUrl = "https://newlink.com" }
            ]
        };

        var result = await _taskService.AddOrUpdateTaskAsync(command, listing.ListingId);

        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.AddedLinks);
        Assert.Single(result.AddedLinks);
        Assert.Equal("New Link", result.AddedLinks[0].LinkText);

        var updatedTask = await _dbContext.Tasks
            .Include(t => t.Links)
            .FirstOrDefaultAsync(t => t.TaskId == task.TaskId);
        Assert.NotNull(updatedTask);
        Assert.Single(updatedTask.Links);
    }

    [Fact]
    public async System.Threading.Tasks.Task AddOrUpdateTaskAsync_UpdateTaskDeleteLink_SoftDeletesLinkSuccessfully()
    {
        var property = _testData.CreateProperty();
        var listing = _testData.CreateListing(property.PropertyId);
        var task = _testData.CreateTask(listing.ListingId, "Task Title", (short)TaskStatus.NotStarted);
        var link = _testData.CreateLink(task.TaskId, "Link to Delete", "https://delete.com");

        var command = new AddOrUpdateTaskCommand
        {
            TaskId = task.TaskId,
            TitleString = "Task Title",
            Room = "Living Room",
            Priority = TaskPriority.Medium,
            Links = [
                new AddOrUpdateLinkRequest
                {
                    LinkId = link.LinkId,
                    LinkText = "Link to Delete",
                    LinkUrl = "https://delete.com",
                    IsMarkedForDeletion = true
                }
            ]
        };

        var result = await _taskService.AddOrUpdateTaskAsync(command, listing.ListingId);

        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        var deletedLink = await _dbContext.Links.FindAsync(link.LinkId);
        Assert.NotNull(deletedLink);
        Assert.NotNull(deletedLink.DeletedAt);
    }

    [Fact]
    public async System.Threading.Tasks.Task AddOrUpdateTaskAsync_UpdateTaskWithMixedLinks_HandlesAddAndDeleteCorrectly()
    {
        var property = _testData.CreateProperty();
        var listing = _testData.CreateListing(property.PropertyId);
        var task = _testData.CreateTask(listing.ListingId, "Task Title", (short)TaskStatus.NotStarted);
        var existingLink = _testData.CreateLink(task.TaskId, "Existing Link", "https://existing.com");

        var command = new AddOrUpdateTaskCommand
        {
            TaskId = task.TaskId,
            TitleString = "Task Title",
            Room = "Living Room",
            Priority = TaskPriority.Medium,
            Links = [
                new AddOrUpdateLinkRequest
                {
                    LinkId = existingLink.LinkId,
                    LinkText = "Existing Link",
                    LinkUrl = "https://existing.com",
                    IsMarkedForDeletion = true
                },
                new AddOrUpdateLinkRequest { LinkText = "New Link", LinkUrl = "https://new.com" }
            ]
        };

        var result = await _taskService.AddOrUpdateTaskAsync(command, listing.ListingId);

        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.AddedLinks);
        Assert.Single(result.AddedLinks);
        Assert.Equal("New Link", result.AddedLinks[0].LinkText);

        var deletedLink = await _dbContext.Links.FindAsync(existingLink.LinkId);
        Assert.NotNull(deletedLink);
        Assert.NotNull(deletedLink.DeletedAt);
    }

    [Fact]
    public async System.Threading.Tasks.Task AddOrUpdateTaskAsync_UpdateNonExistentTask_ReturnsError()
    {
        var command = new AddOrUpdateTaskCommand
        {
            TaskId = 999999,
            TitleString = "Task Title",
            Room = "Living Room",
            Priority = TaskPriority.Medium,
            Links = []
        };

        var result = await _taskService.AddOrUpdateTaskAsync(command, 1);

        Assert.NotNull(result);
        Assert.NotNull(result.ErrorMessage);
        Assert.Equal("Unable to find data", result.ErrorMessage);
    }

    [Fact]
    public async System.Threading.Tasks.Task AddOrUpdateTaskAsync_UpdateTaskDeleteNonExistentLink_IgnoresGracefully()
    {
        var property = _testData.CreateProperty();
        var listing = _testData.CreateListing(property.PropertyId);
        var task = _testData.CreateTask(listing.ListingId, "Task Title", (short)TaskStatus.NotStarted);

        var command = new AddOrUpdateTaskCommand
        {
            TaskId = task.TaskId,
            TitleString = "Task Title",
            Room = "Living Room",
            Priority = TaskPriority.Medium,
            Links = [
                new AddOrUpdateLinkRequest
                {
                    LinkId = 999999,
                    LinkText = "Non-existent Link",
                    LinkUrl = "https://nonexistent.com",
                    IsMarkedForDeletion = true
                }
            ]
        };

        var result = await _taskService.AddOrUpdateTaskAsync(command, listing.ListingId);

        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public async System.Threading.Tasks.Task AddOrUpdateTaskAsync_AddTaskWithNullDescription_CreatesSuccessfully()
    {
        var property = _testData.CreateProperty();
        var listing = _testData.CreateListing(property.PropertyId);

        var command = new AddOrUpdateTaskCommand
        {
            TaskId = null,
            TitleString = "Task without description",
            Room = "Office",
            Description = null,
            Priority = TaskPriority.Low,
            Links = []
        };

        var result = await _taskService.AddOrUpdateTaskAsync(command, listing.ListingId);

        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);
        Assert.True(result.TaskId > 0);

        var createdTask = await _dbContext.Tasks.FindAsync(result.TaskId);
        Assert.NotNull(createdTask);
        Assert.Null(createdTask.Description);
    }

    [Fact]
    public async System.Threading.Tasks.Task AddOrUpdateTaskAsync_UpdateTaskToNullDescription_UpdatesSuccessfully()
    {
        var property = _testData.CreateProperty();
        var listing = _testData.CreateListing(property.PropertyId);
        var task = _testData.CreateTask(listing.ListingId, "Task Title", (short)TaskStatus.NotStarted);

        var command = new AddOrUpdateTaskCommand
        {
            TaskId = task.TaskId,
            TitleString = "Task Title",
            Room = "Living Room",
            Description = null,
            Priority = TaskPriority.Medium,
            Links = []
        };

        var result = await _taskService.AddOrUpdateTaskAsync(command, listing.ListingId);

        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        var updatedTask = await _dbContext.Tasks.FindAsync(task.TaskId);
        Assert.NotNull(updatedTask);
        Assert.Null(updatedTask.Description);
    }

    [Fact]
    public async System.Threading.Tasks.Task AddOrUpdateTaskAsync_AddMultipleLinksWithSimilarUrls_CreatesAllLinks()
    {
        var property = _testData.CreateProperty();
        var listing = _testData.CreateListing(property.PropertyId);

        var command = new AddOrUpdateTaskCommand
        {
            TaskId = null,
            TitleString = "Task with similar links",
            Room = "Garage",
            Priority = TaskPriority.High,
            Links = [
                new AddOrUpdateLinkRequest { LinkText = "Link 1", LinkUrl = "https://example.com/page1" },
                new AddOrUpdateLinkRequest { LinkText = "Link 2", LinkUrl = "https://example.com/page2" },
                new AddOrUpdateLinkRequest { LinkText = "Link 3", LinkUrl = "https://example.com/page3" }
            ]
        };

        var result = await _taskService.AddOrUpdateTaskAsync(command, listing.ListingId);

        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.AddedLinks);
        Assert.Equal(3, result.AddedLinks.Length);
    }

    #endregion

    #region MarkTaskAndChildrenAsDeleted Tests

    [Fact]
    public async System.Threading.Tasks.Task MarkTaskAndChildrenAsDeleted_WithValidTaskId_SoftDeletesTask()
    {
        var property = _testData.CreateProperty();
        var listing = _testData.CreateListing(property.PropertyId);
        var task = _testData.CreateTask(listing.ListingId, "Task to Delete", (short)TaskStatus.NotStarted);

        var result = await _taskService.MarkTaskAndChildrenAsDeleted(task.TaskId);

        Assert.True(result);

        var deletedTask = await _dbContext.Tasks
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TaskId == task.TaskId);
        Assert.NotNull(deletedTask);
        Assert.NotNull(deletedTask.DeletedAt);
    }

    [Fact]
    public async System.Threading.Tasks.Task MarkTaskAndChildrenAsDeleted_WithInvalidTaskId_ReturnsFalse()
    {
        var result = await _taskService.MarkTaskAndChildrenAsDeleted(999999);

        Assert.False(result);
    }

    [Fact]
    public async System.Threading.Tasks.Task MarkTaskAndChildrenAsDeleted_WithLinks_SoftDeletesAllLinks()
    {
        var property = _testData.CreateProperty();
        var listing = _testData.CreateListing(property.PropertyId);
        var task = _testData.CreateTask(listing.ListingId, "Task with Links", (short)TaskStatus.NotStarted);
        var link1 = _testData.CreateLink(task.TaskId, "Link 1", "https://example.com/1");
        var link2 = _testData.CreateLink(task.TaskId, "Link 2", "https://example.com/2");

        var result = await _taskService.MarkTaskAndChildrenAsDeleted(task.TaskId);

        Assert.True(result);

        var deletedLinks = await _dbContext.Links
            .IgnoreQueryFilters()
            .Where(l => l.TaskId == task.TaskId)
            .ToListAsync();

        Assert.Equal(2, deletedLinks.Count);
        Assert.All(deletedLinks, link => Assert.NotNull(link.DeletedAt));
    }

    [Fact]
    public async System.Threading.Tasks.Task MarkTaskAndChildrenAsDeleted_WithFiles_SoftDeletesFilesAndFilesTasks()
    {
        var property = _testData.CreateProperty();
        var listing = _testData.CreateListing(property.PropertyId);
        var task = _testData.CreateTask(listing.ListingId, "Task with Files", (short)TaskStatus.NotStarted);
        var fileType = _testData.CreateFileType("TestType");
        var file1 = _testData.CreateFile(fileType.FileTypeId, ".pdf");
        var file2 = _testData.CreateFile(fileType.FileTypeId, ".jpg");
        var filesTask1 = _testData.CreateFilesTask(file1.FileId, task.TaskId);
        var filesTask2 = _testData.CreateFilesTask(file2.FileId, task.TaskId);

        var result = await _taskService.MarkTaskAndChildrenAsDeleted(task.TaskId);

        Assert.True(result);

        var deletedFilesTasks = await _dbContext.FilesTasks
            .IgnoreQueryFilters()
            .Where(ft => ft.TaskId == task.TaskId)
            .ToListAsync();

        Assert.Equal(2, deletedFilesTasks.Count);
        Assert.All(deletedFilesTasks, ft => Assert.NotNull(ft.DeletedAt));

        var deletedFiles = await _dbContext.Files
            .IgnoreQueryFilters()
            .Where(f => f.FileId == file1.FileId || f.FileId == file2.FileId)
            .ToListAsync();

        Assert.Equal(2, deletedFiles.Count);
        Assert.All(deletedFiles, file => Assert.NotNull(file.DeletedAt));
    }

    [Fact]
    public async System.Threading.Tasks.Task MarkTaskAndChildrenAsDeleted_WithLinksAndFiles_SoftDeletesAll()
    {
        var property = _testData.CreateProperty();
        var listing = _testData.CreateListing(property.PropertyId);
        var task = _testData.CreateTask(listing.ListingId, "Task with Links and Files", (short)TaskStatus.NotStarted);

        var link1 = _testData.CreateLink(task.TaskId, "Link 1", "https://example.com/1");
        var link2 = _testData.CreateLink(task.TaskId, "Link 2", "https://example.com/2");

        var fileType = _testData.CreateFileType("TestType");
        var file = _testData.CreateFile(fileType.FileTypeId, ".pdf");
        var filesTask = _testData.CreateFilesTask(file.FileId, task.TaskId);

        var result = await _taskService.MarkTaskAndChildrenAsDeleted(task.TaskId);

        Assert.True(result);

        var deletedTask = await _dbContext.Tasks
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TaskId == task.TaskId);
        Assert.NotNull(deletedTask);
        Assert.NotNull(deletedTask.DeletedAt);

        var deletedLinks = await _dbContext.Links
            .IgnoreQueryFilters()
            .Where(l => l.TaskId == task.TaskId)
            .ToListAsync();
        Assert.Equal(2, deletedLinks.Count);
        Assert.All(deletedLinks, link => Assert.NotNull(link.DeletedAt));

        var deletedFilesTask = await _dbContext.FilesTasks
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(ft => ft.FileTaskId == filesTask.FileTaskId);
        Assert.NotNull(deletedFilesTask);
        Assert.NotNull(deletedFilesTask.DeletedAt);

        var deletedFile = await _dbContext.Files
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.FileId == file.FileId);
        Assert.NotNull(deletedFile);
        Assert.NotNull(deletedFile.DeletedAt);
    }

    [Fact]
    public async System.Threading.Tasks.Task MarkTaskAndChildrenAsDeleted_DeletedTaskNotReturnedByGetListingTasksAsync()
    {
        var property = _testData.CreateProperty();
        var listing = _testData.CreateListing(property.PropertyId);
        var task1 = _testData.CreateTask(listing.ListingId, "Task 1", (short)TaskStatus.NotStarted);
        var task2 = _testData.CreateTask(listing.ListingId, "Task 2", (short)TaskStatus.InProgress);

        await _taskService.MarkTaskAndChildrenAsDeleted(task1.TaskId);

        var result = await _taskService.GetListingTasksAsync(new ListingTasksQuery(), listing.ListingId);

        Assert.NotNull(result);
        Assert.Single(result.Tasks);
        Assert.Equal(task2.TaskId, result.Tasks[0].TaskId);
        Assert.DoesNotContain(result.Tasks, t => t.TaskId == task1.TaskId);
    }

    [Fact]
    public async System.Threading.Tasks.Task MarkTaskAndChildrenAsDeleted_DeletedTaskWithLinksNotReturnedByGetListingTasksAsync()
    {
        var property = _testData.CreateProperty();
        var listing = _testData.CreateListing(property.PropertyId);
        var task = _testData.CreateTask(listing.ListingId, "Task with Links", (short)TaskStatus.NotStarted);
        var link = _testData.CreateLink(task.TaskId, "Link", "https://example.com");

        await _taskService.MarkTaskAndChildrenAsDeleted(task.TaskId);

        var result = await _taskService.GetListingTasksAsync(new ListingTasksQuery(), listing.ListingId);

        Assert.NotNull(result);
        Assert.Empty(result.Tasks);
    }

    [Fact]
    public async System.Threading.Tasks.Task MarkTaskAndChildrenAsDeleted_DeletedTaskNotIncludedInClientGroupedTasks()
    {
        var agentUser = _testData.CreateUser("agent@test.com", "Agent", "One");
        var clientUser = _testData.CreateUser("client@test.com", "Client", "One");
        var agent = _testData.CreateAgent(agentUser);
        var client = _testData.CreateClient(clientUser);

        var property = _testData.CreateProperty();
        var listing = _testData.CreateListing(property.PropertyId);

        _testData.CreateAgentListing(listing.ListingId, agent.UserId);
        _testData.CreateClientListing(listing.ListingId, client.UserId);

        var task1 = _testData.CreateTask(listing.ListingId, "Task 1", (short)TaskStatus.NotStarted);
        var task2 = _testData.CreateTask(listing.ListingId, "Task 2", (short)TaskStatus.Completed);

        await _taskService.MarkTaskAndChildrenAsDeleted(task1.TaskId);

        var result = await _taskService.GetClientGroupedTasksListAsync(new ClientGroupedTasksListQuery(), agent.UserId);

        Assert.NotNull(result);
        Assert.Single(result.ClientGroupedTasksDetails);

        var group = result.ClientGroupedTasksDetails[0];
        Assert.False(group.TaskStatusCounts.ContainsKey(TaskStatus.NotStarted));
        Assert.Equal(1, group.TaskStatusCounts[TaskStatus.Completed]);
    }

    [Fact]
    public async System.Threading.Tasks.Task MarkTaskAndChildrenAsDeleted_WithNoChildren_SoftDeletesOnlyTask()
    {
        var property = _testData.CreateProperty();
        var listing = _testData.CreateListing(property.PropertyId);
        var task = _testData.CreateTask(listing.ListingId, "Simple Task", (short)TaskStatus.NotStarted);

        var result = await _taskService.MarkTaskAndChildrenAsDeleted(task.TaskId);

        Assert.True(result);

        var deletedTask = await _dbContext.Tasks
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TaskId == task.TaskId);
        Assert.NotNull(deletedTask);
        Assert.NotNull(deletedTask.DeletedAt);
    }

    #endregion

    public void Dispose()
    {
        _testData.Dispose();
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
