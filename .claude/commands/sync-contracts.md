# Sync Contracts from Server to Client

Synchronize all server contracts (DTOs, Commands, Queries, Enums) from the .NET server project to the TypeScript client project, maintaining vertical slice architecture.

## Server Location
- Path: `/home/stew/repos/realtorApp/server/src/RealtorApp.Contracts/`
- Structure:
  - `Enums/` - All enum types
  - `Commands/` - Command requests/responses organized by feature
  - `Queries/` - Query requests/responses organized by feature
  - `Common/` - Shared base types (e.g., ResponseWithError)

## Client Location
- Path: `/home/stew/repos/realtorApp/client/src/app/`
- Structure: Vertical slices per feature (auth, chat, contacts, dashboard, tasks, etc.)
- Each slice has a `models/` folder for its contracts

## Tasks

### 1. Read All Server Contracts
- Read ALL .cs files in RealtorApp.Contracts (exclude obj/Debug files)
- Catalog all enums, commands, queries, and DTOs
- Organize by vertical slice (Auth, Chat, Contacts, Dashboard/Invitations, Tasks, Listing, User)

### 2. Check Client for Each Contract
For each server contract, check if it exists in the client:
- **If found in vertical slice models folder**: No action needed (unless outdated)
- **If found in component/service file**: Move to appropriate models folder
- **If not found**: Create in appropriate vertical slice models folder

### 3. Syncing Rules
- **Keep models aligned with server contracts** - no UI-specific variants
- TypeScript naming: PascalCase for interfaces/enums, camelCase for properties
- C# `required` → TypeScript required (no `?` or `| null`)
- C# nullable (`?`) → TypeScript optional (`?`) or `| null`
- C# arrays `[]` → TypeScript arrays `[]`
- C# `Dictionary<K,V>` → TypeScript `Record<K,V>`
- Enums: Match numeric values exactly
- Responses extending `ResponseWithError`: Use `extends ResponseWithError`

### 4. Model Organization
Create/update files in these locations:
- `/client/src/app/models/common.model.ts` - Common types (ResponseWithError, FileTypes, etc.)
- `/client/src/app/models/user.model.ts` - User-related types
- `/client/src/app/models/listing.model.ts` - Listing-related types
- `/client/src/app/auth/models/auth.model.ts` - Auth commands/responses
- `/client/src/app/chat/models/chat.model.ts` - Chat contracts
- `/client/src/app/contacts/models/contacts.model.ts` - Contacts contracts
- `/client/src/app/dashboard/models/invitations.model.ts` - Invitation contracts
- `/client/src/app/tasks/models/task.model.ts` - Task contracts

### 5. Update Component Imports
After moving/creating models:
- Find all imports of moved types
- Update import paths to reference the new models folder
- Remove old model files if no longer used
- Verify no broken imports remain

### 6. Avoid UI-Specific Variants
- Do NOT create UI-specific interfaces (e.g., `*UI`, `*WithLabel`)
- Use Angular pipes in templates for formatting (date pipe, etc.)
- Keep models pure and aligned with server contracts

## Output
Provide a summary of:
- New model files created
- Existing files updated
- Component imports updated
- Old files removed
- Any contracts that couldn't be synced (with reasons)
