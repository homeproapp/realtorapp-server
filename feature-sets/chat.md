# Chat Feature Set

## Overview
This document defines the real-time chat functionality between agents and clients for property-related conversations.

## Current State
- SignalR ChatHub exists with basic structure
- JWT authentication configured for SignalR
- Database has Conversations, Messages, and Attachments tables
- UserAuthService has conversation participant validation
- SendMessage method is commented out and needs implementation

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

### File Attachments
- **Object References**: Attach references to other app objects (not traditional files)
- **Task References**: Attach task objects to messages
- **Future Extensions**: Support for other object types (properties, contacts, etc.)
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
- GET `/conversations?limit=20&offset=0` - GetConversationListQuery

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
- **Conversations Table**: Existing structure
- **ConversationsProperties Table**: Links conversations to properties

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