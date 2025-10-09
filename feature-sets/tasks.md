# Tasks Feature Set

## Overview
This document defines the task management system where agents create and manage tasks for property improvements, maintenance, and client follow-ups, while clients can contribute details and update task status.

## Current State
- ✅ Database schema exists with Tasks, TaskAttachments, FilesTask, Links, and related models
- 🔄 **Phase 1 Pending**: Task CRUD operations and client interaction implementation

## Requirements

### Task Management Flow
1. **Agent Creation**: Agent creates tasks for specific properties with detailed information
2. **Client Collaboration**: Clients can view tasks, add details, and update status
3. **Image Attachments**: Tasks can have multiple images attachments for documentation
4. **Link Management**: Tasks can include referral links with usage tracking

### Task Data Structure
Based on existing schema, tasks contain:
- **Basic Information**: Title, room location, priority, status
- **Financial Details**: Estimated cost for budgeting
- **Timeline**: Follow-up dates for scheduling
- **Property Association**: Linked to specific property
- **File Attachments**: Multiple files via FilesTask relationship
- **Web Links**: Related URLs with referral tracking via Links table

### User Permissions

**Agent Permissions (Full CRUD):**
- ✅ Create new tasks for any property they manage
- ✅ Read all task details and history
- ✅ Update any task field (title, room, priority, status, cost, dates)
- ✅ Delete tasks (soft delete)
- ✅ Manage file attachments and links

**Client Permissions (Limited):**
- ✅ Read tasks for properties they're associated with
- ✅ Update task status only
- ✅ Add comments/notes to tasks (new field needed)
- ✅ View attached files and links
- ❌ Cannot create, delete, or modify other task fields

### Task Status & Priority

**Task Status (short field):**
- `0` - Not Started
- `1` - In Progress
- `2` - Completed
- `3` - On Hold
- `4` - Cancelled

**Task Priority (short field):**
- `1` - Low
- `2` - Medium
- `3` - High
- `4` - Urgent

### Task Categories/Rooms
Common room/area categories:
- Kitchen, Living Room, Master Bedroom, Bathroom, Basement
- Exterior, Landscaping, Garage, General/Other

## API Endpoints

### Task CRUD (Agent)
- GET `/api/tasks/v1/property/{propertyId}` - Get all tasks for a property
- GET `/api/tasks/v1/task/{taskId}` - Get specific task details
- POST `/api/tasks/v1/task` - Create new task
- PUT `/api/tasks/v1/task/{taskId}` - Update task (agent full access)
- DELETE `/api/tasks/v1/task/{taskId}/delete` - Soft delete task (agent only)

### Task Interaction (Client)
<!-- - GET `/api/tasks/v1/my-tasks` - Get client's tasks across all properties -->
- PUT `/api/tasks/v1/{taskId}/status` - Update task status only (client)
<!-- - POST `/api/tasks/v1/{taskId}/comments` - Add comment to task (client) -->

### Task Attachments
- POST `/api/tasks/v1/{taskId}/files` - Upload file attachment
- DELETE `/api/tasks/v1/{taskId}/files/{fileId}` - Remove file attachment
- POST `/api/tasks/v1/{taskId}/links` - Add web link
- PUT `/api/tasks/v1/{taskId}/links/{linkId}` - Update link details
- DELETE `/api/tasks/v1/{taskId}/links/{linkId}` - Remove link

## Request/Response Examples

### POST /api/tasks/v1 (Create Task)
```json
{
  "propertyId": 123,
  "title": "Replace kitchen faucet",
  "room": "Kitchen",
  "priority": 2,
  "status": 0,
  "estimatedCost": 25000,
  "followUpDate": "2024-02-15T00:00:00Z",
  "description": "Client mentioned faucet is leaking and needs replacement",
  "links": [
    {
      "name": "Home Depot Faucet Options",
      "url": "https://www.homedepot.ca/search?q=kitchen+faucet",
      "isReferral": true
    }
  ]
}
```

### GET /api/tasks/v1/{taskId} Response
```json
{
  "taskId": 789,
  "propertyId": 123,
  "title": "Replace kitchen faucet",
  "room": "Kitchen",
  "priority": 2,
  "status": 1,
  "estimatedCost": 25000,
  "followUpDate": "2024-02-15T00:00:00Z",
  "description": "Client mentioned faucet is leaking and needs replacement",
  "createdAt": "2024-01-15T10:00:00Z",
  "updatedAt": "2024-01-16T14:30:00Z",
  "files": [
    {
      "fileId": 101,
      "uuid": "550e8400-e29b-41d4-a716-446655440000",
      "fileName": "faucet_invoice.pdf",
      "fileExtension": "pdf",
      "fileType": "document",
      "uploadedAt": "2024-01-16T09:00:00Z"
    }
  ],
  "links": [
    {
      "linkId": 201,
      "name": "Home Depot Faucet Options",
      "url": "https://www.homedepot.ca/search?q=kitchen+faucet&ref=agent123",
      "isReferral": true,
      "timesUsed": 3,
      "createdAt": "2024-01-15T10:00:00Z"
    }
  ],
  "comments": [
    {
      "commentId": 301,
      "userId": 789,
      "userType": "client",
      "userName": "John Smith",
      "comment": "I've gotten quotes from two contractors, Mario's quote looks reasonable",
      "createdAt": "2024-01-16T11:30:00Z"
    }
  ]
}
```

### PUT /api/tasks/v1/{taskId}/status (Client Update)
```json
{
  "status": 2,
  "comment": "Work has been completed, waiting for agent review"
}
```

## Database Schema Changes

### Tasks Table Updates
```sql
-- Add description field for detailed task notes
ALTER TABLE tasks ADD COLUMN description TEXT;

-- Add created_by field to track which agent created the task
ALTER TABLE tasks ADD COLUMN created_by INTEGER REFERENCES agents(agent_id);

-- Add indexes for performance
CREATE INDEX idx_tasks_property_id ON tasks(property_id) WHERE deleted_at IS NULL;
CREATE INDEX idx_tasks_status ON tasks(status) WHERE deleted_at IS NULL;
CREATE INDEX idx_tasks_priority ON tasks(priority) WHERE deleted_at IS NULL;
CREATE INDEX idx_tasks_follow_up_date ON tasks(follow_up_date) WHERE deleted_at IS NULL;
```

### Task Comments Table (New)
```sql
CREATE TABLE task_comments (
    comment_id SERIAL PRIMARY KEY,
    task_id BIGINT NOT NULL REFERENCES tasks(task_id),
    user_id INTEGER NOT NULL REFERENCES users(user_id),
    comment TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at TIMESTAMPTZ
);

CREATE INDEX idx_task_comments_task_id ON task_comments(task_id) WHERE deleted_at IS NULL;
CREATE INDEX idx_task_comments_user_id ON task_comments(user_id) WHERE deleted_at IS NULL;
```

### Task Titles Table Enhancement
```sql
-- The existing task_titles table appears to be a lookup table for common task titles
-- Consider adding more standard tasks for real estate context
INSERT INTO task_titles (task_title_1) VALUES
    ('Replace HVAC filter'),
    ('Fix leaky faucet'),
    ('Paint interior walls'),
    ('Repair drywall'),
    ('Clean gutters'),
    ('Service furnace'),
    ('Update light fixtures'),
    ('Replace flooring'),
    ('Landscaping maintenance'),
    ('Roof inspection');
```

## Service Architecture

### ITaskService
**Responsibilities:**
- Manage task CRUD operations with proper authorization
- Handle file and link attachment management
- Enforce client vs agent permission boundaries
- Generate task analytics and reporting

**Key Methods:**
- `GetTasksByPropertyAsync(propertyId, userId, userRole)` - Get property tasks with role-based filtering
- `GetTaskByIdAsync(taskId, userId, userRole)` - Get specific task with authorization
- `CreateTaskAsync(createTaskCommand, agentId)` - Create new task (agent only)
- `UpdateTaskAsync(updateTaskCommand, userId, userRole)` - Update with permission validation
- `UpdateTaskStatusAsync(taskId, status, userId, comment)` - Client status updates
- `DeleteTaskAsync(taskId, agentId)` - Soft delete (agent only)
- `AddTaskCommentAsync(taskId, userId, comment)` - Add comment to task
- `AttachFileToTaskAsync(taskId, fileId, userId)` - File attachment management

### ITaskAuthorizationService
**Responsibilities:**
- Validate user permissions for task operations
- Check property-user relationships for access control
- Enforce client vs agent permission boundaries
- Audit task access attempts

**Key Methods:**
- `CanUserAccessTaskAsync(taskId, userId, userRole)` - General access validation
- `CanUserModifyTaskAsync(taskId, userId, userRole)` - Modification permission check
- `CanUserDeleteTaskAsync(taskId, userId, userRole)` - Deletion permission check
- `IsTaskInUserPropertyAsync(taskId, userId)` - Property relationship validation

### ITaskNotificationService
**Responsibilities:**
- Send notifications for task updates and status changes
- Notify agents of client comments and status updates
- Handle follow-up date reminders
- Generate task completion notifications

**Key Methods:**
- `NotifyTaskCreatedAsync(taskId, clientIds)` - New task notifications
- `NotifyTaskUpdatedAsync(taskId, changes, notifyUsers)` - Update notifications
- `NotifyTaskCommentAsync(taskId, comment, recipientIds)` - Comment notifications
- `SendFollowUpRemindersAsync()` - Scheduled reminder service

## Contract Structure (RealtorApp.Contracts)

### Commands
**Commands/Tasks/Requests/**
- `CreateTaskCommand`
- `UpdateTaskCommand`
- `UpdateTaskStatusCommand`
- `DeleteTaskCommand`
- `AddTaskCommentCommand`
- `AttachFileToTaskCommand`

**Commands/Tasks/Responses/**
- `CreateTaskCommandResponse`
- `UpdateTaskCommandResponse`
- `UpdateTaskStatusCommandResponse`
- `DeleteTaskCommandResponse`
- `AddTaskCommentCommandResponse`

### Queries
**Queries/Tasks/Requests/**
- `GetTasksByPropertyQuery`
- `GetTaskByIdQuery`
- `GetMyTasksQuery` (client view)
- `GetTaskCommentsQuery`

**Queries/Tasks/Responses/**
- `GetTasksByPropertyQueryResponse`
- `GetTaskByIdQueryResponse`
- `GetMyTasksQueryResponse`
- `GetTaskCommentsQueryResponse`

### Supporting Models
**Tasks/**
- `TaskResponse`
- `TaskSummaryResponse`
- `TaskCommentResponse`
- `TaskFileResponse`
- `TaskLinkResponse`

### Enums
**Enums/**
```csharp
public enum TaskStatus
{
    NotStarted = 0,
    InProgress = 1,
    PendingReview = 2,
    Completed = 3,
    OnHold = 4,
    Cancelled = 5
}

public enum TaskPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Urgent = 4
}
```

## Security Considerations

### Access Control
- **Property-based Authorization**: Users can only access tasks for properties they're associated with
- **Role-based Permissions**: Strict enforcement of agent vs client capabilities
- **Task Ownership**: Agents can only modify tasks they created
- **File Access**: Secure file attachment access with proper authorization

### Data Validation
- **Input Sanitization**: Clean all text inputs, especially comments and descriptions
- **File Upload Security**: Validate file types, sizes, and scan for malicious content
- **URL Validation**: Verify link URLs and sanitize for XSS prevention
- **Permission Boundaries**: Prevent privilege escalation attempts

### Audit Trail
- **Change Tracking**: Log all task modifications with user attribution
- **Comment History**: Maintain complete comment thread history
- **File Operations**: Track file uploads, downloads, and deletions
- **Access Logging**: Log task access attempts for security monitoring

## Real-time Features (SignalR Integration)

### Task Update Notifications
- **Live Status Updates**: Real-time status changes for all property participants
- **Comment Notifications**: Instant comment alerts to relevant users
- **File Upload Alerts**: Notify when new files are attached to tasks
- **Assignment Updates**: Alert when tasks are updated or completed

### SignalR Hub Methods
TODO: - this will be a more generic notification hub set of methods that will handle task updates and others
  - If user is connected to websocket, they receive live in app notification
  - if they are not:
    - mobile receives notification record and push notification
    - web receivs notification record
<!-- - `JoinTaskRoom(taskId)` - Subscribe to task updates
- `LeaveTaskRoom(taskId)` - Unsubscribe from task updates
- `NotifyTaskUpdated(taskId, updateType, userId)` - Broadcast task changes
- `NotifyTaskComment(taskId, comment, userId)` - Broadcast new comments -->

## Integration Points

### Chat System Integration
- **Task References**: Reference tasks in chat messages via attachments

### Product Search Integration
- **Link Generation**: Add product search results as task links
- **Referral Tracking**: Track link usage for commission purposes
- **Cost Estimation**: Use product prices for task cost estimates

## Success Criteria
- **Performance**: Task list loads in under 2 seconds for properties with 100+ tasks
- **User Experience**: Intuitive task creation/editing with proper validation
- **Real-time Updates**: Live notifications within 1 second of task changes
- **Mobile Responsiveness**: Full task management capability on mobile devices
- **Data Integrity**: Zero data loss with proper soft delete and audit trails
- **Permission Enforcement**: 100% accuracy in role-based access control

## UI/UX Requirements

### Listing Header Component (Shared)
The tasks page and conversation page both need a consistent header showing listing details with the ability to switch between listings. This should be extracted into a reusable component.

**Component: `ListingHeaderComponent`**
Location: `src/app/components/shared/page/listing-header/`

**Features:**
- Display client avatar(s) (single or stacked for multiple clients)
- Show client name(s) (comma-separated for multiple)
- Display listing address
- Listing selector dropdown (icon-only select with home icon)
  - Only shown when user has multiple listings
  - Lists all other available listings for the same clients
- Back button integration (handled by parent page)
- Loading skeleton state

**Inputs:**
- `listingDetails: ListingDetails` - Contains clientNames, address, otherListings
- `loading: boolean` - Show skeleton during data fetch

**Outputs:**
- `listingChange: EventEmitter<number>` - Emits selected listing ID

**Usage Example:**
```html
<ion-header>
  <ion-toolbar>
    <ion-buttons slot="start">
      <ion-back-button [defaultHref]="'/tasks'"></ion-back-button>
    </ion-buttons>
    <app-listing-header
      [listingDetails]="listingDetails()"
      [loading]="loading()"
      (listingChange)="handleListingChange($event)"
    />
  </ion-toolbar>
</ion-header>
```

**Migration:**
- Replace `app-conversation-header` in `/chat/pages/conversation/conversation.page.html` with new `app-listing-header`
- Use same component in tasks listing page

### Tasks Listing Page
**Route:** `/tasks/:listingId`

**Layout:**
1. **Header Section**
   - Listing header component (described above)
   - Task completion summary cards/stats by room and priority

2. **Task List Section**
   - Grouped by room (collapsible sections)
   - Task cards showing:
     - Title
     - Status indicator (color-coded)
     - Priority badge
     - Estimated cost
     - Follow-up date (if set)
     - Attachment count indicator
     - Link count indicator
   - Pull-to-refresh support
   - Infinite scroll for pagination

3. **Action Button**
   - Floating action button (FAB) to create new task (agent only)
   - Positioned bottom-right

**Filtering & Sorting:**
- Client-side filtering by:
  - Room
  - Status
  - Priority
- Sort options:
  - Created date (newest/oldest)
  - Follow-up date
  - Priority (high to low)
  - Status

## Implementation Phases

### Phase 1: Core Task Management
- Extract and create shared `ListingHeaderComponent` from conversation header
- Basic CRUD operations for agents
- Client status update functionality
- File attachment management
- Simple task listing page with grouping by room
- Task detail view/edit page

### Phase 2: Enhanced Collaboration
- Advanced task filtering and search
  - for now will be handled client side
- Task completion analytics display
- Pull-to-refresh and infinite scroll

### Phase 3: Advanced Features
- Task analytics and reporting
- Automated follow-up reminders
- Task templates for common scenarios
- Bulk task operations
- Mobile app optimization