# Sync Contracts from Server to Client

Synchronize server contracts (DTOs, Commands, Queries, Enums) from the .NET server project to the TypeScript client project, maintaining vertical slice architecture.

**IMPORTANT**: This uses git to detect changed contracts for efficient incremental syncing!

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

## Workflow (GIT-BASED INCREMENTAL SYNC)

### 1. Detect Changed Contracts (Git-Based)
Use git to find only the contracts that have changed:

**Check uncommitted/staged changes:**
```bash
git status --porcelain -- src/RealtorApp.Contracts/
```

**Check recent commits (last 5 commits by default):**
```bash
git diff --name-only HEAD~5 -- src/RealtorApp.Contracts/
```

**Result:** List of changed .cs files in Contracts directory

**If no changes detected:** Report "No contract changes detected" and exit early.

**For full sync:** If user requests full sync or this is first time, skip git check and process all contracts.

### 2. Extract Contract Names from Changed Files
- ONLY read the changed .cs files identified in step 1
- Extract class/interface/enum names from each changed file
- These are the contracts that need syncing

### 3. Sync Changed Contracts to Client
For each changed contract:
- Determine which client model file it belongs to (based on mapping below)
- Read the server contract definition
- Check if it exists in the client model file:
  - **Exists**: Update/replace the existing interface/enum
  - **New**: Add to the appropriate section of the file
- Update the client model file

### 4. Validation
After syncing changed contracts:
- Verify all imports are correct
- Check for any broken references
- Report what was added/updated

### 5. Git-Based Options

**Default behavior:** Sync changes from last 5 commits + uncommitted changes

**User can request:**
- "sync contracts" - Default behavior (last 5 commits)
- "sync contracts full" - Full sync ignoring git (scan all contracts)
- "sync contracts since [commit-hash]" - Sync changes since specific commit
- "sync contracts last [N] commits" - Sync changes from last N commits

### 6. Syncing Rules
- **Keep models aligned with server contracts** - no UI-specific variants
- TypeScript naming: PascalCase for interfaces/enums, camelCase for properties
- C# `required` → TypeScript required (no `?` or `| null`)
- C# nullable (`?`) → TypeScript optional (`?`) or `| null`
- C# arrays `[]` → TypeScript arrays `[]`
- C# `Dictionary<K,V>` → TypeScript `Record<K,V>`
- Enums: Match numeric values exactly
- Responses extending `ResponseWithError`: Use `extends ResponseWithError`

### 7. Model Organization
Create/update files in these locations:
- `/client/src/app/models/common.model.ts` - Common types (ResponseWithError, FileTypes, etc.)
- `/client/src/app/models/user.model.ts` - User-related types
- `/client/src/app/models/listing.model.ts` - Listing-related types
- `/client/src/app/auth/models/auth.model.ts` - Auth commands/responses
- `/client/src/app/chat/models/chat.model.ts` - Chat contracts
- `/client/src/app/contacts/models/contacts.model.ts` - Contacts contracts
- `/client/src/app/dashboard/models/invitations.model.ts` - Invitation contracts
- `/client/src/app/tasks/models/task.model.ts` - Task contracts

### 8. Update Component Imports (if needed)
After moving/creating models:
- Find all imports of moved types
- Update import paths to reference the new models folder
- Remove old model files if no longer used
- Verify no broken imports remain

### 9. Avoid UI-Specific Variants
- Do NOT create UI-specific interfaces (e.g., `*UI`, `*WithLabel`)
- Use Angular pipes in templates for formatting (date pipe, etc.)
- Keep models pure and aligned with server contracts

## Output
Provide a concise summary:
- **Git scan results**: How many changed files detected (e.g., "3 changed contracts in last 5 commits")
- **Synced contracts**: List which interfaces/enums were added/updated in which client files
- **No changes**: If git detected no changes, report "No contract changes detected - all in sync"
- **Skipped**: Any contracts that couldn't be synced (with reasons)

## Benefits of Git-Based Approach
- **Faster**: Only processes changed files instead of scanning entire Contracts directory
- **Efficient**: Skips unchanged contracts entirely
- **Smart**: Detects both committed and uncommitted changes
- **Flexible**: Can sync specific commit ranges or do full sync when needed
- **Early exit**: If no changes, exits immediately without scanning client files
