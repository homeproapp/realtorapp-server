# Sync Contracts from Server to Client

Synchronize all server contracts (DTOs, Commands, Queries, Enums) from the .NET server project to the TypeScript client project, maintaining vertical slice architecture.

**IMPORTANT**: This is an incremental sync - only add/update contracts that are missing or changed, don't recreate everything!

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

## Workflow (INCREMENTAL APPROACH)

### 1. Find All Server Contracts
- List ALL .cs files in RealtorApp.Contracts (exclude obj/Debug/bin files)
- Extract just the class/interface/enum names from each file (don't read full content yet)

### 2. Check What Already Exists in Client
- Read each existing client model file
- Extract all exported interface/enum names
- Create a map of what already exists

### 3. Identify Missing/New Contracts
Compare server contracts against client models:
- **Already exists in client**: Skip (assume in sync unless you have reason to believe otherwise)
- **Missing from client**: Read the server contract and add to appropriate client model file
- **Suspicious (recently modified server file)**: Check if client version matches

### 4. Only Sync What's Needed
- ONLY read server contract files that have missing/new types
- ONLY update client model files that need new contracts
- Don't touch files that are already in sync

### 5. Syncing Rules
- **Keep models aligned with server contracts** - no UI-specific variants
- TypeScript naming: PascalCase for interfaces/enums, camelCase for properties
- C# `required` → TypeScript required (no `?` or `| null`)
- C# nullable (`?`) → TypeScript optional (`?`) or `| null`
- C# arrays `[]` → TypeScript arrays `[]`
- C# `Dictionary<K,V>` → TypeScript `Record<K,V>`
- Enums: Match numeric values exactly
- Responses extending `ResponseWithError`: Use `extends ResponseWithError`

### 6. Model Organization
Create/update files in these locations:
- `/client/src/app/models/common.model.ts` - Common types (ResponseWithError, FileTypes, etc.)
- `/client/src/app/models/user.model.ts` - User-related types
- `/client/src/app/models/listing.model.ts` - Listing-related types
- `/client/src/app/auth/models/auth.model.ts` - Auth commands/responses
- `/client/src/app/chat/models/chat.model.ts` - Chat contracts
- `/client/src/app/contacts/models/contacts.model.ts` - Contacts contracts
- `/client/src/app/dashboard/models/invitations.model.ts` - Invitation contracts
- `/client/src/app/tasks/models/task.model.ts` - Task contracts

### 7. Update Component Imports (if needed)
After moving/creating models:
- Find all imports of moved types
- Update import paths to reference the new models folder
- Remove old model files if no longer used
- Verify no broken imports remain

### 8. Avoid UI-Specific Variants
- Do NOT create UI-specific interfaces (e.g., `*UI`, `*WithLabel`)
- Use Angular pipes in templates for formatting (date pipe, etc.)
- Keep models pure and aligned with server contracts

## Output
Provide a concise summary of ONLY what changed:
- **New contracts added**: List which interfaces/enums were added to which files
- **Files updated**: Which client model files were modified
- **Already in sync**: Brief note if everything was already up to date
- **Skipped**: Any contracts that couldn't be synced (with reasons)

**Do NOT list everything that already existed** - focus only on the delta/changes made.
