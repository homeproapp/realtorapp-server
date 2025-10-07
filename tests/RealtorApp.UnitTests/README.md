# Unit Tests Documentation

## Overview

This project uses **xUnit** as the testing framework with a real PostgreSQL database for integration testing. Tests are designed to validate business logic in the Domain layer services.

## Test Setup Architecture

### Database Configuration

- Tests use a **real PostgreSQL database** configured in `appsettings.json`
- Each test class creates a new DbContext instance connected to the test database
- Database is cleaned before each test to ensure test isolation

### Key Components

#### 1. TestBase (`Services/TestBase.cs`)

An abstract base class that provides:
- Pre-configured `RealtorAppDbContext` connected to test database
- Mock service instances (IEmailService, IUserService, etc.)
- `InvitationService` initialized with mocks
- `TestDataManager` for creating test data
- Automatic cleanup of all test data after each test

**Usage:**
```csharp
public class MyServiceTests : TestBase
{
    public MyServiceTests()
    {
        // Additional setup if needed
    }

    [Fact]
    public async Task MyTest()
    {
        // Use DbContext, MockServices, TestDataManager
    }
}
```

#### 2. TestDataManager (`Helpers/TestDataManager.cs`)

A helper class for creating test data with automatic cleanup:
- Tracks all created entities by their IDs
- Provides factory methods for common entities (Users, Agents, Clients, Properties, Tasks, etc.)
- Automatically deletes all created entities when disposed
- Generates unique IDs and emails to prevent conflicts

**Available Factory Methods:**
- `CreateUser(email, firstName, lastName, uuid?)` → User
- `CreateAgent(user)` → Agent
- `CreateClient(user)` → Client
- `CreateProperty(...)` → Property
- `CreateListing(propertyId, title?)` → Listing
- `CreateTask(listingId, title?, status?, updatedAt?)` → Task
- `CreateLink(taskId, name?, url?)` → Link
- `CreateConversation(listingId)` → Conversation
- `CreateMessage(listingId, senderId, text)` → Message
- And more...

#### 3. TestExtensions (`Services/TestExtensions.cs`)

Extension methods for common test assertions:
- `IsSuccess()` methods for checking command responses

### Service-Specific Test Classes

Some services require their own setup and don't inherit from TestBase:

**Example: TaskServiceTests**
```csharp
public class TaskServiceTests : IDisposable
{
    private readonly RealtorAppDbContext _dbContext;
    private readonly TaskService _taskService;
    private TestDataManager _testData;

    public TaskServiceTests()
    {
        // Manual DbContext setup
        // Initialize service under test
        // Create TestDataManager
    }

    public void Dispose()
    {
        _testData.Dispose();
        _dbContext.Dispose();
    }
}
```

## How to Add New Tests

### Option 1: Using TestBase (Recommended for services with many dependencies)

1. Create a new test class that inherits from `TestBase`:

```csharp
public class MyServiceTests : TestBase
{
    private readonly MyService _myService;

    public MyServiceTests()
    {
        _myService = new MyService(
            DbContext,
            MockEmailService.Object,
            // Add other dependencies
        );
    }

    [Fact]
    public async Task MyMethod_WithValidInput_ReturnsExpectedResult()
    {
        // Arrange
        var agent = CreateTestAgent();
        var client = CreateTestClient();

        // Act
        var result = await _myService.MyMethod(agent.UserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedValue, result.SomeProperty);
    }
}
```

### Option 2: Standalone Test Class (For services with fewer dependencies)

1. Create a test class implementing `IDisposable`:

```csharp
public class MyServiceTests : IDisposable
{
    private readonly RealtorAppDbContext _dbContext;
    private readonly MyService _myService;
    private TestDataManager _testData;

    public MyServiceTests()
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
        _myService = new MyService(_dbContext);
    }

    private void CleanupAllTestData()
    {
        _dbContext.Database.ExecuteSqlRaw(@"
            DELETE FROM [child_tables];
            DELETE FROM [parent_tables];
        ");
    }

    [Fact]
    public async Task MyTest()
    {
        // Test implementation
    }

    public void Dispose()
    {
        _testData.Dispose();
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
```

### Test Organization Pattern

Organize tests using regions for clarity:

```csharp
#region GetSomethingAsync Tests

[Fact]
public async Task GetSomethingAsync_WithValidData_ReturnsExpectedResult()
{
    // ...
}

[Fact]
public async Task GetSomethingAsync_WithInvalidData_ReturnsNull()
{
    // ...
}

#endregion

#region Test Data Setup

private async Task<long> SetupTestData_ForSpecificScenario()
{
    var user = _testData.CreateUser("test@example.com", "First", "Last");
    var agent = _testData.CreateAgent(user);
    return await Task.FromResult(agent.UserId);
}

#endregion
```

## Best Practices

### 1. Test Data Setup
- Use `TestDataManager` factory methods instead of creating entities manually
- Create helper methods for complex test scenarios (e.g., `SetupTestData_MultipleClientsOneListing()`)
- Use descriptive names that explain the scenario being tested

### 2. Async/Await
- Always use `async Task` for async tests
- Use full namespace for Task return type: `System.Threading.Tasks.Task`
- Example: `public async System.Threading.Tasks.Task MyTest()`

### 3. Assertions
- Use xUnit's `Assert` methods
- Be specific with assertions (e.g., `Assert.Equal(expected, actual)` instead of `Assert.True(actual == expected)`)
- Test both success and failure cases

### 4. Test Naming
Follow the pattern: `MethodName_Scenario_ExpectedBehavior`
- `GetListingTasksAsync_WithNoTasks_ReturnsEmptyArray`
- `AddOrUpdateTaskAsync_UpdateNonExistentTask_ReturnsError`
- `MarkTaskAsDeleted_WithValidTaskId_SoftDeletesTaskAndChildren`

### 5. Cleanup
- Always implement `IDisposable` if not using TestBase
- Dispose `TestDataManager` and `DbContext` in the correct order
- Use `GC.SuppressFinalize(this)` in Dispose method

### 6. Database State
- Never assume database state from other tests
- Each test should be independent and self-contained
- Clean up happens automatically via TestDataManager

## Running Tests

```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~TaskServiceTests"

# Run specific test
dotnet test --filter "FullyQualifiedName~TaskServiceTests.GetListingTasksAsync_WithNoTasks_ReturnsEmptyArray"

# Run with verbose output
dotnet test -v detailed
```

## Adding New Factory Methods to TestDataManager

If you need to create a new type of test entity:

1. Add a tracking list:
```csharp
private readonly List<long> _createdMyEntityIds = new();
```

2. Add a factory method:
```csharp
public MyEntity CreateMyEntity(params...)
{
    var id = _nextUserId++;
    var entity = new MyEntity
    {
        MyEntityId = id,
        // Set properties
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
    _dbContext.MyEntities.Add(entity);
    _dbContext.SaveChanges();
    _createdMyEntityIds.Add(id);
    return entity;
}
```

3. Add cleanup in Dispose method (respecting foreign key order):
```csharp
if (_createdMyEntityIds.Any())
{
    _dbContext.MyEntities.RemoveRange(
        _dbContext.MyEntities.Where(e => _createdMyEntityIds.Contains(e.MyEntityId))
    );
}
```

## Common Patterns

### Testing Soft Deletes
```csharp
[Fact]
public async Task DeleteMethod_SoftDeletesEntity()
{
    var entity = _testData.CreateEntity();

    await _service.DeleteMethod(entity.Id);

    var deleted = await _dbContext.Entities.FindAsync(entity.Id);
    Assert.NotNull(deleted);
    Assert.NotNull(deleted.DeletedAt);
}
```

### Testing Global Query Filters
```csharp
[Fact]
public async Task GetMethod_DoesNotReturnSoftDeletedEntities()
{
    var entity = _testData.CreateEntity();
    await _service.DeleteMethod(entity.Id);

    var result = await _service.GetAllEntities();

    Assert.DoesNotContain(result, e => e.Id == entity.Id);
}
```

### Testing with Multiple Related Entities
```csharp
[Fact]
public async Task Method_WithRelatedEntities_HandlesCorrectly()
{
    var parent = _testData.CreateParent();
    var child1 = _testData.CreateChild(parent.Id);
    var child2 = _testData.CreateChild(parent.Id);

    var result = await _service.GetParentWithChildren(parent.Id);

    Assert.Equal(2, result.Children.Count);
}
```
