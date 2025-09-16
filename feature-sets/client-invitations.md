# Client Invitations Feature Set

## Overview
This document defines the client invitation system where agents can invite clients to join the platform, creating their user accounts and establishing property/agent relationships.

## Current State
- Users, Agents, Clients tables exist
- ClientsProperties table exists for agent-client-property relationships
- Email system needs to be implemented

## Requirements

### Invitation Flow
1. **Agent Invites Client**: Agent provides client email and assigns properties
2. **User Creation**: System creates User and Client records immediately
3. **Relationship Setup**: Agent-client-property relationships established
4. **Email Notification**: Client receives invitation email
5. **Client Registration**: Client completes Firebase registration and first login

### Key Challenge: UUID/Firebase UID Handling
**Problem**: Users table requires `uuid` field, but clients don't have Firebase UID until they register.

**Common Solutions**:
- Generate temporary UUID, replace with Firebase UID on registration
- Use nullable UUID field, populate on first login
- Use separate invitation tokens, link to Firebase UID later
- Pre-generate Firebase custom tokens for invited users

### Invitation Process
- **Create User Record**: With temporary identifier or nullable UUID
- **Property Assignment**: Link client to properties via ClientsProperties
- **Agent Assignment**: Establish agent-client relationship
- **Email Generation**: Send invitation link with registration token
- **Registration Completion**: Link Firebase UID to existing user record

## API Endpoints
- POST `/invitations/send` - Agent invites client(s) to property
- GET `/invitations/{token}` - Validate invitation token
- POST `/invitations/accept` - Complete client registration with Firebase token

## Database Changes
- Consider nullable `uuid` field in Users table
- Add `Invitations` table:
  - `invitation_id` (primary key)
  - `user_id` (foreign key to created User)
  - `invitation_token` (unique token for email link)
  - `invited_by` (agent user ID)
  - `expires_at` (invitation expiry)
  - `accepted_at` (completion timestamp)
  - `created_at`, `updated_at`

## Email Integration
- Email service for sending invitation emails
- Email templates for new vs existing user scenarios
- Invitation link generation with secure tokens

## Security Considerations
- Secure invitation token generation
- Token expiration and single-use validation
- Prevent unauthorized property access before registration
- Rate limiting on invitation endpoints

## Edge Cases
- Client already has Firebase account with different email
- Invitation expires before client registers
- Agent tries to invite same client multiple times
- Client tries to register without invitation