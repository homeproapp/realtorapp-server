# Chat Feature Set

## Overview
This document defines the real-time chat functionality between agents and clients for property-related conversations.

## Current State

### ✅ Completed Implementation
- **SignalR ChatHub**: Basic structure with JWT authentication configured
- **Database Schema**: Conversations, Messages, and Attachments tables exist
  - **Schema Simplification**: Removed conversations_properties table, added property_id directly to conversations table
- **UserAuthService**: Conversation participant validation exists
- **Contract Classes**: All required CQRS command/query contracts implemented
  - `SendMessageCommand` and `SendMessageCommandResponse` (exists)
  - `MarkMessagesAsReadCommand` and `MarkMessagesAsReadCommandResponse` (created)
  - `GetMessageHistoryQuery` and `GetMessageHistoryQueryResponse` (created)
  - `GetConversationListQuery` and `GetConversationListQueryResponse` (created)
  - Supporting models: `MessageResponse`, `ConversationResponse`, `ClientConversationResponse`
- **ChatService**: Business logic layer implemented with PostgreSQL raw SQL queries
  - `SendMessageAsync` method implemented
  - `GetAgentConversationListAsync` with sophisticated client grouping logic
  - `MarkMessagesAsReadAsync` method implemented
- **SQL Query Infrastructure**: Embedded resources system for complex queries
  - Raw SQL queries stored in `src/RealtorApp.Domain/SqlQueries/Chat/`
  - `ISqlQueryService` and `SqlQueryService` for managing SQL resources
- **Extension Methods**: `ChatExtensions.cs` with mapping helpers
  - `ToClientConversationResponses()` for client list mapping
- **Test Infrastructure**: PostgreSQL-based testing with TestDataManager
  - Automatic cleanup and unique data generation
  - Support for real database testing instead of in-memory limitations

### 🚧 In Progress
- **Test Fixes**: Updating remaining test files to use TestDataManager approach
  - ✅ ResendInvitation tests fixed
  - ✅ SendInvitations tests fixed
  - ⏳ AcceptInvitation, ValidateInvitation, and EdgeCase tests pending

### ⏳ Pending Implementation
- **SendMessage method in ChatHub**: Integration with IChatService
- **Chat API Controllers**: CQRS pattern with proper validation
- **SignalR Hub Methods**: Message history, read receipts, typing indicators
- **End-to-end Testing**: Complete chat workflow validation

## Requirements

### Real-time Messaging
- **SignalR Hub**: Use existing ChatHub for real-time communication
- **Agent-Client Communication**: 1 agent with 1 or many clients per conversation
- **Property-based Conversations**: Each conversation tied to specific property
- **Multiple Property Support**: Same agent-client pair can have multiple conversations (one per property)

### Message History & Persistence
- **Initial Load**: Load last 50 messages on conversation open (newest first)
- **Cursor-based Pagination**: Use timestamp cursor for efficient scrolling backward
  - `limit`: Number of messages to return (default: 50, max: 100)
  - `before`: Timestamp cursor for loading older messages
- **Database Storage**: Persist all messages in Messages table
- **Message Ordering**: Newest messages first, reverse chronological order
- **Pagination Response**: Include `hasMore` flag and `nextBefore` cursor

### Message Attachments
- **Object References**: Attach references to other app objects (not traditional files)
- **Task References**: Attach task objects to messages
- **Future Extensions**: Support for other object types (properties, contacts, files, etc.)
- **Attachment Metadata**: Store attachment type and reference ID

### Read Receipts
- **Simple Implementation**: Use existing `is_read` column in Messages table
- **Bulk Mark as Read**: Support marking multiple messages as read in single operation
- **Use Cases**:
  - User opens chat and marks all unread messages as read
  - User scrolls through messages and marks viewed messages as read
  - Efficiency for multiple unread messages since last visit
- **Read Status Display**: Show read/unread status in UI

### Typing Indicators
- **Real-time Updates**: Use existing SetTyping method in ChatHub
- **Participant Validation**: Ensure only conversation participants can send typing indicators
- **Temporary State**: Typing indicators are ephemeral (not persisted)

### Conversation Structure
- **Property-centric**: Conversations belong to specific property
- **Agent-Client Relationship**: Managed via ClientsProperties table
- **Participant Validation**: Use existing UserAuthService.IsConversationParticipant

## API Endpoints (CQRS Pattern)

### Commands
- POST `/conversations/{conversationId}/messages` - SendMessageCommand
- PUT `/messages/read` - MarkMessagesAsReadCommand

### Queries
- GET `/conversations/{conversationId}/messages?limit=50&before={timestamp}` - GetMessageHistoryQuery
- GET `/conversations?limit=20&offset=0` - GetConversationListQuery (Agent-specific grouping)

**NOTE**: Client conversation list implementation needs to be designed and addressed separately from agent conversation list.

## Contract Structure (RealtorApp.Contracts)

### Commands
**Commands/Chat/Requests/**
- `SendMessageCommand` (exists)
- `MarkMessagesAsReadCommand`

**Commands/Chat/Responses/**
- `SendMessageCommandResponse` (exists)
- `MarkMessagesAsReadCommandResponse`

### Queries
**Queries/Chat/Requests/**
- `GetMessageHistoryQuery`
  - `ConversationId` (from route)
  - `Limit` (default: 50, max: 100)
  - `Before` (optional timestamp for pagination)
- `GetConversationListQuery`
  - `Limit` (default: 20, max: 50)
  - `Offset` (default: 0)

**Queries/Chat/Responses/**
- `GetMessageHistoryQueryResponse`
  - `Messages` (array of MessageResponse)
  - `HasMore` (boolean indicating more messages available)
  - `NextBefore` (timestamp for next page)
- `GetConversationListQueryResponse`
  - `Conversations` (array of ConversationResponse)
  - `TotalCount` (total conversations for user)
  - `HasMore` (boolean indicating more pages)

### Supporting Models
**Chat/**
- `MessageResponse`
- `ConversationResponse`
- `AttachmentRequest` (exists)
- `AttachmentResponse` (exists)

## SignalR Hub Methods
- `JoinConversation(conversationId, propertyId)` - Join conversation group
- `LeaveConversation(conversationId)` - Leave conversation group
- `SendMessage(conversationId, propertyId, messageText, attachments)` - Send message
- `SetTyping(conversationId, propertyId, isTyping)` - Typing indicator
- `MarkAsRead(messageIds[])` - Mark multiple messages as read
- `MarkAsRead(messageId)` - Mark single message as read (overload)

## SignalR Client Events
- `onMessage` - Receive new message
- `onTyping` - Receive typing indicator
- `onMessageRead` - Receive read receipt update

## Database Schema
- **Messages Table**: Existing structure with `is_read` column
- **Attachments Table**: Existing structure for message attachments
- **Conversations Table**: Existing structure with `property_id` field (simplified from bridge table)
- **ClientsProperties Table**: Links clients to properties and agents (manages conversation membership)

## Technical Implementation Notes

### Agent Conversation Grouping
The `GetAgentConversationListAsync` method implements sophisticated grouping logic:
- **Client Set Grouping**: Conversations with identical client sets are grouped together
- **Property Separation**: Same clients on different properties create separate conversation groups
- **Raw SQL Performance**: Uses PostgreSQL-specific features for efficient grouping and aggregation
- **Unread Count**: Counts conversations (not messages) with unread content per group

### Database Design Decision
**Simplified Schema**: Removed `conversations_properties` bridge table in favor of direct `property_id` on conversations table:
- **Benefit**: Simpler queries and reduced joins
- **Assumption**: Each conversation belongs to exactly one property (validated by business requirements)
- **Migration**: Existing bridge table logic can be migrated to direct property relationship

### SQL Query Management
**Embedded Resources Pattern**:
- Complex queries stored as `.sql` files in `SqlQueries/Chat/` directory
- `ISqlQueryService` provides clean abstraction for loading SQL from resources
- Enables version control, syntax highlighting, and maintainability for complex PostgreSQL queries
- Alternative to fragile string concatenation or ORM limitations for advanced queries

### Test Infrastructure Innovations
**TestDataManager Approach**:
- Dynamic ID generation using timestamps to avoid conflicts
- Automatic cleanup tracking - only removes data that tests created
- Real PostgreSQL database instead of in-memory providers (supports raw SQL)
- Unique email generation to prevent constraint violations
- Backward compatibility with existing test helper methods

## Security Considerations
- **Participant Validation**: Verify user is conversation participant before any action
- **Property Access**: Validate user has access to property via ClientsProperties
- **Message Authorization**: Only participants can read/send messages
- **Attachment Validation**: Verify attachment references exist and user has access

## Out of Scope (MVP)
- Message editing
- Message deletion
- Traditional file uploads
- Message reactions/emoji
- Message threading