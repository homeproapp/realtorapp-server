# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a .NET 9.0 realtor application server with a clean architecture pattern:
- **RealtorApp.Api**: Web API layer with controllers, hubs, and validators
- **RealtorApp.Domain**: Business logic layer with services, models, and data access
- **RealtorApp.Contracts**: DTOs and contracts for API communication

## Technology Stack

- **.NET 9.0** with C# (nullable reference types enabled)
- **Entity Framework Core 9.0** with PostgreSQL (Npgsql provider)
- **SignalR** for real-time chat functionality
- **JWT Bearer authentication** with user validation
- **FluentValidation** for request validation
- **Memory caching** for performance optimization

## Development Commands

### Build and Run
```bash
# Build the solution
dotnet build

# Run the API project
dotnet run --project src/RealtorApp.Api

# Restore dependencies
dotnet restore
```

### Database Commands
```bash
# Add a new migration
dotnet ef migrations add <MigrationName> --project src/RealtorApp.Domain --startup-project src/RealtorApp.Api

# Update database
dotnet ef database update --project src/RealtorApp.Domain --startup-project src/RealtorApp.Api

# Remove last migration
dotnet ef migrations remove --project src/RealtorApp.Domain --startup-project src/RealtorApp.Api
```

### Testing
```bash
# Run all tests (if test projects exist)
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Architecture Patterns

### Domain Model
The application centers around real estate management with these core entities:
- **Users** (base table for both agents and clients)
- **Agents** and **Clients** (inherit from Users)
- **Properties** with location, pricing, and metadata
- **Conversations** and **Messages** for agent-client communication
- **Tasks** with attachments and third-party contacts

### Database Design
- Uses PostgreSQL with `citext` and `pgcrypto` extensions
- Snake_case column naming convention
- Soft deletes via `deleted_at` timestamps
- Audit fields: `created_at`, `updated_at`
- Partial indexes for active records only

### Caching Strategy
The `UserAuthService` implements multi-layer caching:
- User ID by UUID lookup
- Conversation participants
- Property user assignments
- Configurable expiration times via `appsettings.json`

### Authentication & Authorization
- JWT Bearer tokens for API authentication
- SignalR hub authorization with token from query string
- User validation through `IUserAuthService`
- Email validation checks for agents

### Real-time Communication
SignalR `ChatHub` provides:
- Connection management with user tracking
- Conversation group membership
- Typing indicators
- Participant validation before joining conversations

## Code Conventions

### Project Settings
- `TreatWarningsAsErrors` is enabled across all projects
- Nullable reference types are enabled
- Implicit usings are enabled

### Error Messages
- **ALWAYS keep error messages vague** for security
- Use unique error codes for internal debugging (documented in `docs/error-codes.md`)
- Never expose specific validation failures or internal system details to API consumers

### Dependency Injection
- **ALWAYS initialize DI services from constructor into private readonly fields with `_` prefix**
- Use primary constructor pattern when possible
- Example: `private readonly IService _service = service;`

### JWT Claims Access
- **ALWAYS use `ClaimTypes` constants instead of raw string claim names**
- Use `ClaimTypes.NameIdentifier` for "sub" claim (user UUID)
- Use `ClaimTypes.Role` for "role" claim (user role)
- This handles Microsoft's claim URI mapping automatically
- Example: `User.FindFirst(ClaimTypes.NameIdentifier)?.Value` instead of `User.FindFirst("sub")?.Value`

### Entity Framework
- Database-first approach with scaffolded DbContext
- Foreign key constraints with restrict delete behavior
- Proper async/await patterns with `ConfigureAwait(false)`
- **IMPORTANT**: `ExecuteUpdateAsync` and `ExecuteDeleteAsync` bypass EF Core's change tracker
  - When testing code that uses `ExecuteUpdateAsync`, you MUST call `DbContext.ChangeTracker.Clear()` before querying the updated entity
  - Without clearing the tracker, queries will return stale cached values instead of the updated database values
  - Example:
    ```csharp
    await _context.ThirdPartyContacts
        .Where(c => c.Id == id)
        .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.Name, "New Name"));

    _dbContext.ChangeTracker.Clear(); // Required!

    var updated = await _context.ThirdPartyContacts.FirstAsync(c => c.Id == id);
    // Now 'updated.Name' will be "New Name"
    ```

### Service Pattern
- Interface-based dependency injection
- Primary constructor pattern (C# 12)
- Scoped lifetime for database-dependent services

## Configuration

### Connection Strings
Database connection configured in `appsettings.json` under `ConnectionStrings:Default`

### Cache Settings
```json
{
  "UserIdCacheExpirationInMins": 20,
  "ConversationParticipantsCacheExpirationInMins": 20,
  "UsersAssignedToPropertyCacheExpirationInMins": 20
}
```

### Security
- User secrets configured for development environment
- JWT configuration in authentication pipeline
- HTTPS redirection enabled

## Important Notes

- The `SendMessage` method in `ChatHub` is commented out and needs implementation
- Entity models are auto-generated from database schema
- Memory cache is used extensively for performance-critical lookups
- All database queries use `AsNoTracking()` for read-only operations