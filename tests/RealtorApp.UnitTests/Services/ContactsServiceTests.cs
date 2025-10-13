using Microsoft.EntityFrameworkCore;
using RealtorApp.Contracts.Commands.Contacts.Requests;
using RealtorApp.Domain.Services;

namespace RealtorApp.UnitTests.Services;

public class ContactsServiceTests : TestBase
{
    private readonly ContactsService _contactsService;

    public ContactsServiceTests()
    {
        _contactsService = new ContactsService(DbContext);
    }

    #region GetThirdPartyContactsAsync Tests

    [Fact]
    public async System.Threading.Tasks.Task GetThirdPartyContactsAsync_WithNoContacts_ReturnsEmptyArray()
    {
        var user = TestDataManager.CreateUser("agent@example.com", "Agent", "Smith");
        var agent = TestDataManager.CreateAgent(user);

        var result = await _contactsService.GetThirdPartyContactsAsync(agent.UserId);

        Assert.NotNull(result);
        Assert.NotNull(result.ThirdPartyContacts);
        Assert.Empty(result.ThirdPartyContacts);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetThirdPartyContactsAsync_WithMultipleContacts_ReturnsAllContacts()
    {
        var user = TestDataManager.CreateUser("agent@example.com", "Agent", "Smith");
        var agent = TestDataManager.CreateAgent(user);

        var contact1 = TestDataManager.CreateThirdPartyContact(agent.UserId, "Electrician Co", "electric@test.com", "+1111111111", "Electrician");
        var contact2 = TestDataManager.CreateThirdPartyContact(agent.UserId, "Plumber Co", "plumber@test.com", "+2222222222", "Plumber");
        var contact3 = TestDataManager.CreateThirdPartyContact(agent.UserId, "HVAC Co", "hvac@test.com", "+3333333333", "HVAC");

        var result = await _contactsService.GetThirdPartyContactsAsync(agent.UserId);

        Assert.NotNull(result);
        Assert.NotNull(result.ThirdPartyContacts);
        Assert.Equal(3, result.ThirdPartyContacts.Length);
        Assert.Contains(result.ThirdPartyContacts, c => c.ThirdPartyId == contact1.ThirdPartyContactId);
        Assert.Contains(result.ThirdPartyContacts, c => c.ThirdPartyId == contact2.ThirdPartyContactId);
        Assert.Contains(result.ThirdPartyContacts, c => c.ThirdPartyId == contact3.ThirdPartyContactId);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetThirdPartyContactsAsync_WithMultipleAgents_ReturnsOnlyAgentContacts()
    {
        var user1 = TestDataManager.CreateUser("agent1@example.com", "Agent", "One");
        var agent1 = TestDataManager.CreateAgent(user1);
        var user2 = TestDataManager.CreateUser("agent2@example.com", "Agent", "Two");
        var agent2 = TestDataManager.CreateAgent(user2);

        var contact1 = TestDataManager.CreateThirdPartyContact(agent1.UserId, "Agent1 Contact", "contact1@test.com", "+1111111111", "Electrician");
        var contact2 = TestDataManager.CreateThirdPartyContact(agent2.UserId, "Agent2 Contact", "contact2@test.com", "+2222222222", "Plumber");

        var result = await _contactsService.GetThirdPartyContactsAsync(agent1.UserId);

        Assert.NotNull(result);
        Assert.NotNull(result.ThirdPartyContacts);
        Assert.Single(result.ThirdPartyContacts);
        Assert.Equal(contact1.ThirdPartyContactId, result.ThirdPartyContacts[0].ThirdPartyId);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetThirdPartyContactsAsync_MapsPropertiesCorrectly()
    {
        var user = TestDataManager.CreateUser("agent@example.com", "Agent", "Smith");
        var agent = TestDataManager.CreateAgent(user);
        var contact = TestDataManager.CreateThirdPartyContact(agent.UserId, "Test Contractor", "test@contractor.com", "+9876543210", "Carpenter");

        var result = await _contactsService.GetThirdPartyContactsAsync(agent.UserId);

        Assert.Single(result.ThirdPartyContacts);
        var returned = result.ThirdPartyContacts[0];
        Assert.Equal(contact.ThirdPartyContactId, returned.ThirdPartyId);
        Assert.Equal("Test Contractor", returned.Name);
        Assert.Equal("Carpenter", returned.Service);
        Assert.Equal("test@contractor.com", returned.Email);
        Assert.Equal("+9876543210", returned.PhoneNumber);
    }

    #endregion

    #region GetThirdPartyContactAsync Tests

    [Fact]
    public async System.Threading.Tasks.Task GetThirdPartyContactAsync_WithValidId_ReturnsContact()
    {
        var user = TestDataManager.CreateUser("agent@example.com", "Agent", "Smith");
        var agent = TestDataManager.CreateAgent(user);
        var contact = TestDataManager.CreateThirdPartyContact(agent.UserId, "Test Contractor", "test@contractor.com", "+9876543210", "Carpenter");

        var result = await _contactsService.GetThirdPartyContactAsync(contact.ThirdPartyContactId, agent.UserId);

        Assert.NotNull(result);
        Assert.NotNull(result.ThirdPartyContact);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(contact.ThirdPartyContactId, result.ThirdPartyContact.ThirdPartyId);
        Assert.Equal("Test Contractor", result.ThirdPartyContact.Name);
        Assert.Equal("Carpenter", result.ThirdPartyContact.Service);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetThirdPartyContactAsync_WithInvalidId_ReturnsErrorMessage()
    {
        var user = TestDataManager.CreateUser("agent@example.com", "Agent", "Smith");
        var agent = TestDataManager.CreateAgent(user);

        var result = await _contactsService.GetThirdPartyContactAsync(999999, agent.UserId);

        Assert.NotNull(result);
        Assert.Null(result.ThirdPartyContact);
        Assert.Equal("No third party contact found", result.ErrorMessage);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetThirdPartyContactAsync_MapsPropertiesCorrectly()
    {
        var user = TestDataManager.CreateUser("agent@example.com", "Agent", "Smith");
        var agent = TestDataManager.CreateAgent(user);
        var contact = TestDataManager.CreateThirdPartyContact(agent.UserId, "Full Mapping Test", "mapping@test.com", "+1234567890", "Electrician");

        var result = await _contactsService.GetThirdPartyContactAsync(contact.ThirdPartyContactId, agent.UserId);

        Assert.NotNull(result.ThirdPartyContact);
        Assert.Equal(contact.ThirdPartyContactId, result.ThirdPartyContact.ThirdPartyId);
        Assert.Equal("Full Mapping Test", result.ThirdPartyContact.Name);
        Assert.Equal("Electrician", result.ThirdPartyContact.Service);
        Assert.Equal("mapping@test.com", result.ThirdPartyContact.Email);
        Assert.Equal("+1234567890", result.ThirdPartyContact.PhoneNumber);
    }

    #endregion

    #region AddOrUpdateThirdPartyContactAsync - Add Tests

    [Fact]
    public async System.Threading.Tasks.Task AddOrUpdateThirdPartyContactAsync_WithNullId_CreatesNewContact()
    {
        var user = TestDataManager.CreateUser("agent@example.com", "Agent", "Smith");
        var agent = TestDataManager.CreateAgent(user);

        var command = new AddOrUpdateThirdPartyContactCommand
        {
            ThirdPartyId = null,
            Name = "New Contractor",
            Email = "new@contractor.com",
            PhoneNumber = "+1111111111",
            Service = "Plumber"
        };

        var result = await _contactsService.AddOrUpdateThirdPartyContactAsync(command, agent.UserId);

        Assert.NotNull(result);
        Assert.NotEqual(0, result.ThirdPartyId);
        Assert.Equal("New Contractor", result.Name);
        Assert.Equal("new@contractor.com", result.Email);
        Assert.Equal("+1111111111", result.PhoneNumber);
        Assert.Equal("Plumber", result.Service);

        var dbContact = await DbContext.ThirdPartyContacts.FindAsync(result.ThirdPartyId);
        Assert.NotNull(dbContact);
        Assert.Equal(agent.UserId, dbContact.AgentId);
        Assert.Equal("New Contractor", dbContact.Name);
        Assert.Equal("Plumber", dbContact.Trade);
    }

    [Fact]
    public async System.Threading.Tasks.Task AddOrUpdateThirdPartyContactAsync_WithNullId_MapsAllFieldsCorrectly()
    {
        var user = TestDataManager.CreateUser("agent@example.com", "Agent", "Smith");
        var agent = TestDataManager.CreateAgent(user);

        var command = new AddOrUpdateThirdPartyContactCommand
        {
            ThirdPartyId = null,
            Name = "Field Test",
            Email = "fields@test.com",
            PhoneNumber = "+9999999999",
            Service = "HVAC"
        };

        var result = await _contactsService.AddOrUpdateThirdPartyContactAsync(command, agent.UserId);

        var dbContact = await DbContext.ThirdPartyContacts.FindAsync(result.ThirdPartyId);
        Assert.NotNull(dbContact);
        Assert.Equal("Field Test", dbContact.Name);
        Assert.Equal("fields@test.com", dbContact.Email);
        Assert.Equal("+9999999999", dbContact.Phone);
        Assert.Equal("HVAC", dbContact.Trade);
        Assert.Equal(agent.UserId, dbContact.AgentId);
    }

    #endregion

    #region AddOrUpdateThirdPartyContactAsync - Update Tests

    [Fact]
    public async System.Threading.Tasks.Task AddOrUpdateThirdPartyContactAsync_WithExistingId_UpdatesContact()
    {
        var user = TestDataManager.CreateUser("agent@example.com", "Agent", "Smith");
        var agent = TestDataManager.CreateAgent(user);
        var contact = TestDataManager.CreateThirdPartyContact(agent.UserId, "Original Name", "original@test.com", "+1111111111", "Electrician");

        var command = new AddOrUpdateThirdPartyContactCommand
        {
            ThirdPartyId = contact.ThirdPartyContactId,
            Name = "Updated Name",
            Email = "updated@test.com",
            PhoneNumber = "+2222222222",
            Service = "Plumber",
            IsMarkedForDeletion = false
        };

        var result = await _contactsService.AddOrUpdateThirdPartyContactAsync(command, agent.UserId);

        Assert.NotNull(result);
        Assert.Equal(contact.ThirdPartyContactId, result.ThirdPartyId);
        Assert.Equal("Updated Name", result.Name);
        Assert.Equal("updated@test.com", result.Email);
        Assert.Equal("+2222222222", result.PhoneNumber);
        Assert.Equal("Plumber", result.Service);

        DbContext.ChangeTracker.Clear();

        var dbContact = await DbContext.ThirdPartyContacts
            .FirstOrDefaultAsync(c => c.ThirdPartyContactId == contact.ThirdPartyContactId);
        Assert.NotNull(dbContact);
        Assert.Equal("Updated Name", dbContact.Name);
        Assert.Equal("updated@test.com", dbContact.Email);
        Assert.Equal("+2222222222", dbContact.Phone);
        Assert.Equal("Plumber", dbContact.Trade);
    }

    [Fact]
    public async System.Threading.Tasks.Task AddOrUpdateThirdPartyContactAsync_WithExistingId_UpdatesUpdatedAtTimestamp()
    {
        var user = TestDataManager.CreateUser("agent@example.com", "Agent", "Smith");
        var agent = TestDataManager.CreateAgent(user);
        var contact = TestDataManager.CreateThirdPartyContact(agent.UserId, "Original", "original@test.com", "+1111111111", "Electrician");

        var originalUpdatedAt = contact.UpdatedAt;
        await System.Threading.Tasks.Task.Delay(100);

        var command = new AddOrUpdateThirdPartyContactCommand
        {
            ThirdPartyId = contact.ThirdPartyContactId,
            Name = "Updated",
            Email = "updated@test.com",
            PhoneNumber = "+2222222222",
            Service = "Plumber",
            IsMarkedForDeletion = false
        };

        await _contactsService.AddOrUpdateThirdPartyContactAsync(command, agent.UserId);

        DbContext.ChangeTracker.Clear();

        var dbContact = await DbContext.ThirdPartyContacts
            .FirstOrDefaultAsync(c => c.ThirdPartyContactId == contact.ThirdPartyContactId);
        Assert.NotNull(dbContact);
        Assert.True(dbContact.UpdatedAt > originalUpdatedAt);
    }

    [Fact]
    public async System.Threading.Tasks.Task AddOrUpdateThirdPartyContactAsync_WithMarkedForDeletion_SoftDeletesContact()
    {
        var user = TestDataManager.CreateUser("agent@example.com", "Agent", "Smith");
        var agent = TestDataManager.CreateAgent(user);
        var contact = TestDataManager.CreateThirdPartyContact(agent.UserId, "To Delete", "delete@test.com", "+1111111111", "Electrician");

        var command = new AddOrUpdateThirdPartyContactCommand
        {
            ThirdPartyId = contact.ThirdPartyContactId,
            Name = "To Delete",
            Email = "delete@test.com",
            PhoneNumber = "+1111111111",
            Service = "Electrician",
            IsMarkedForDeletion = true
        };

        await _contactsService.AddOrUpdateThirdPartyContactAsync(command, agent.UserId);

        DbContext.ChangeTracker.Clear();

        var dbContact = await DbContext.ThirdPartyContacts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.ThirdPartyContactId == contact.ThirdPartyContactId);
        Assert.NotNull(dbContact);
        Assert.NotNull(dbContact.DeletedAt);
    }

    [Fact]
    public async System.Threading.Tasks.Task AddOrUpdateThirdPartyContactAsync_WithMarkedForDeletion_DoesNotUpdateOtherFields()
    {
        var user = TestDataManager.CreateUser("agent@example.com", "Agent", "Smith");
        var agent = TestDataManager.CreateAgent(user);
        var contact = TestDataManager.CreateThirdPartyContact(agent.UserId, "Original Name", "original@test.com", "+1111111111", "Electrician");

        var command = new AddOrUpdateThirdPartyContactCommand
        {
            ThirdPartyId = contact.ThirdPartyContactId,
            Name = "This Should Not Update",
            Email = "should-not-update@test.com",
            PhoneNumber = "+9999999999",
            Service = "Wrong Service",
            IsMarkedForDeletion = true
        };

        await _contactsService.AddOrUpdateThirdPartyContactAsync(command, agent.UserId);

        DbContext.ChangeTracker.Clear();

        var dbContact = await DbContext.ThirdPartyContacts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.ThirdPartyContactId == contact.ThirdPartyContactId);
        Assert.NotNull(dbContact);
        Assert.NotNull(dbContact.DeletedAt);
        Assert.Equal("Original Name", dbContact.Name);
        Assert.Equal("original@test.com", dbContact.Email);
        Assert.Equal("+1111111111", dbContact.Phone);
        Assert.Equal("Electrician", dbContact.Trade);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async System.Threading.Tasks.Task AddOrUpdateThirdPartyContactAsync_WithNullFields_HandlesGracefully()
    {
        var user = TestDataManager.CreateUser("agent@example.com", "Agent", "Smith");
        var agent = TestDataManager.CreateAgent(user);

        var command = new AddOrUpdateThirdPartyContactCommand
        {
            ThirdPartyId = null,
            Name = null,
            Email = null,
            PhoneNumber = null,
            Service = null
        };

        var result = await _contactsService.AddOrUpdateThirdPartyContactAsync(command, agent.UserId);

        Assert.NotNull(result);
        var dbContact = await DbContext.ThirdPartyContacts.FindAsync(result.ThirdPartyId);
        Assert.NotNull(dbContact);
        Assert.Null(dbContact.Name);
        Assert.Null(dbContact.Email);
        Assert.Null(dbContact.Phone);
        Assert.Null(dbContact.Trade);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetThirdPartyContactsAsync_WithSoftDeletedContacts_DoesNotReturnDeleted()
    {
        var user = TestDataManager.CreateUser("agent@example.com", "Agent", "Smith");
        var agent = TestDataManager.CreateAgent(user);

        var activeContact = TestDataManager.CreateThirdPartyContact(agent.UserId, "Active", "active@test.com", "+1111111111", "Electrician");
        var deletedContact = TestDataManager.CreateThirdPartyContact(agent.UserId, "Deleted", "deleted@test.com", "+2222222222", "Plumber");

        await DbContext.ThirdPartyContacts
            .Where(c => c.ThirdPartyContactId == deletedContact.ThirdPartyContactId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.DeletedAt, DateTime.UtcNow));

        var result = await _contactsService.GetThirdPartyContactsAsync(agent.UserId);

        Assert.NotNull(result);
        Assert.Single(result.ThirdPartyContacts);
        Assert.Equal(activeContact.ThirdPartyContactId, result.ThirdPartyContacts[0].ThirdPartyId);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetThirdPartyContactAsync_WithSoftDeletedContact_ReturnsNull()
    {
        var user = TestDataManager.CreateUser("agent@example.com", "Agent", "Smith");
        var agent = TestDataManager.CreateAgent(user);
        var contact = TestDataManager.CreateThirdPartyContact(agent.UserId, "Deleted", "deleted@test.com", "+1111111111", "Electrician");

        await DbContext.ThirdPartyContacts
            .Where(c => c.ThirdPartyContactId == contact.ThirdPartyContactId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.DeletedAt, DateTime.UtcNow));

        var result = await _contactsService.GetThirdPartyContactAsync(contact.ThirdPartyContactId, agent.UserId);

        Assert.NotNull(result);
        Assert.Null(result.ThirdPartyContact);
        Assert.Equal("No third party contact found", result.ErrorMessage);
    }

    [Fact]
    public async System.Threading.Tasks.Task AddOrUpdateThirdPartyContactAsync_UpdateNonExistentContact_ExecutesWithoutError()
    {
        var user = TestDataManager.CreateUser("agent@example.com", "Agent", "Smith");
        var agent = TestDataManager.CreateAgent(user);

        var command = new AddOrUpdateThirdPartyContactCommand
        {
            ThirdPartyId = 999999,
            Name = "Non-existent",
            Email = "none@test.com",
            PhoneNumber = "+1111111111",
            Service = "Electrician",
            IsMarkedForDeletion = false
        };

        var result = await _contactsService.AddOrUpdateThirdPartyContactAsync(command, agent.UserId);

        Assert.NotNull(result);
        Assert.Equal(999999, result.ThirdPartyId);
    }

    #endregion
}
