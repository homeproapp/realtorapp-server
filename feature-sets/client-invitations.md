# Client Invitations Feature Set

## Overview
This document defines the client invitation system where agents can invite clients to join the platform, creating their user accounts and establishing property/agent relationships.

## Current State
- ✅ **Phase 1 Complete**: Invitation creation and email system fully implemented
- ✅ Database schema updated with invitation tables
- ✅ Email service with Amazon SES integration and encrypted invitation links
- ✅ Rate limiting and validation implemented
- ✅ **Phase 2 Complete**: Invitation acceptance and user record creation fully implemented
- ✅ **Phase 3 Complete**: Re-invite functionality for failed invitations or client detail updates fully implemented

## Requirements

### Invitation Flow
1. **Agent Bulk Invitation**: Agent provides multiple client details and multiple property assignments
2. **Invitation Record Creation**: System creates ClientInvitation and PropertyInvitation records with relationships
3. **Email Notification**: Each client receives individual invitation email with unique token
4. **Client Registration**: Client clicks invitation link and completes Firebase registration
5. **Record Creation**: Upon acceptance, system creates actual User, Client, Property, and ClientsProperties records

### Bulk Invitation Details
**Agent Form Structure:**
- **Client Details Section**: Agent can add multiple clients with:
  - Email (required)
  - First Name (optional)
  - Last Name (optional)
  - Phone (optional)
- **Property Details Section**: Agent can add multiple properties with:
  - Address Line 1 (required)
  - Address Line 2 (optional)
  - City (required)
  - Region (required)
  - Postal Code (required)
  - Country Code (required)
- **Relationship Creation**: All clients will be associated with all created properties

**Processing Logic (Invitation Creation):**
- ✅ Validate agent exists and get agent name
- ✅ Create PropertyInvitation records for each property with address details
- ✅ Create ClientInvitation records for each client with their details
- ✅ Create ClientInvitationsProperties records for every client-property combination
- ✅ Generate unique invitation tokens for each client (UUID)
- ✅ Check for existing users to determine invitation type
- ✅ Send personalized invitation emails using Amazon SES
- ✅ Use encrypted invitation links with crypto service
- ✅ Handle email failures and report to agent
- ✅ Each client can accept their invitation independently

**Processing Logic (Invitation Acceptance):**
- ✅ Validate invitation token and Firebase authentication
- ✅ Create actual User and Client records from ClientInvitation data
- ✅ Create actual Property records from PropertyInvitation data
- ✅ Create ClientsProperties relationships from ClientInvitationsProperties
- ✅ Mark invitation as accepted and link to created User record
- ✅ Generate JWT access and refresh tokens for immediate login

**Processing Logic (Re-Invite):**
- ✅ Validate existing ClientInvitation record exists and is not accepted
- ✅ Generate new invitation token (UUID) to replace existing token
- ✅ Update invitation expiry date to extend validity period (7 days from re-invite)
- ✅ Update any changed client details (email, firstName, lastName, phone)
- ✅ Preserve existing property assignments and relationships
- ✅ Send new invitation email with updated token and client information
- ✅ Handle email delivery failures and report success/failure to agent

### Key Challenge: UUID/Firebase UID Handling
**Problem**: Users table requires `uuid` field, but clients don't have Firebase UID until they register.

**✅ Implemented Solution**: Use nullable UUID field approach
- Users table `uuid` field is now nullable
- ClientInvitation records store client details without creating User records
- Upon invitation acceptance, User record is created with Firebase UID
- ClientInvitation.created_user_id links to the created User record

### Invitation Process
- **Create User Record**: With temporary identifier or nullable UUID
- **Property Assignment**: Link client to properties via ClientsProperties
- **Agent Assignment**: Establish agent-client relationship
- **Email Generation**: Send invitation link with registration token
- **Registration Completion**: Link Firebase UID to existing user record

## API Endpoints
- POST `/api/invitations/v1/send` - Agent invites multiple clients to multiple properties
- GET `/api/invitations/v1/validate` - Validate invitation token
- POST `/api/invitations/v1/accept` - Complete client registration with Firebase token
- PUT `/api/invitations/v1/resend` - Re-send invitation with updated details and new token

### POST /invitations/send Request Structure
```json
{
  "clients": [
    {
      "email": "client1@example.com",
      "firstName": "John",
      "lastName": "Doe",
      "phone": "+1234567890"
    },
    {
      "email": "client2@example.com",
      "firstName": "Jane",
      "lastName": "Smith"
    }
  ],
  "properties": [
    {
      "addressLine1": "123 Main Street",
      "addressLine2": "Apt 4B",
      "city": "Toronto",
      "region": "ON",
      "postalCode": "M5V 3A8",
      "countryCode": "CA",
      "listPrice": 750000.00,
      "bedrooms": 2,
      "bathrooms": 2,
      "propertyType": "Condo"
    },
    {
      "addressLine1": "456 Oak Avenue",
      "city": "Vancouver",
      "region": "BC",
      "postalCode": "V6B 1A1",
      "countryCode": "CA",
      "listPrice": 1200000.00,
      "bedrooms": 3,
      "bathrooms": 2,
      "propertyType": "House"
    }
  ]
}
```

**Result**: Creates all client-property combinations:
- Client1 ↔ Property(123 Main St), Property(456 Oak Ave)
- Client2 ↔ Property(123 Main St), Property(456 Oak Ave)
- Total: 4 ClientsProperties records + 2 Invitations sent

### PUT /api/invitations/v1/resend Request Structure
```json
{
  "clientInvitationId": 123,
  "clientDetails": {
    "email": "updated_client@example.com",
    "firstName": "UpdatedJohn",
    "lastName": "UpdatedDoe",
    "phone": "+1234567891"
  }
}
```

**Response Structure**:
```json
{
  "success": true,
  "errorMessage": null
}
```

**Processing**:
- ✅ Validates agent owns the invitation and invitation is not yet accepted
- ✅ Updates ClientInvitation record with new client details
- ✅ Generates new invitation_token and extends expires_at (7 days from re-invite)
- ✅ Preserves all existing property relationships
- ✅ Determines existing user status for proper email template
- ✅ Sends new invitation email with updated information and new encrypted token
- ✅ Returns success/failure status with error details if applicable

## Database Changes
- ✅ Made `uuid` field nullable in Users table
- ✅ Added `ClientInvitations` table:
  - `client_invitation_id` (primary key)
  - `client_email`, `client_first_name`, `client_last_name`, `client_phone` (invited client details)
  - `invitation_token` (unique token for email link)
  - `invited_by` (foreign key to agents table)
  - `expires_at` (invitation expiry)
  - `accepted_at` (completion timestamp)
  - `created_user_id` (foreign key to Users table when accepted)
  - `created_at`, `updated_at`, `deleted_at`
- ✅ Added `PropertyInvitations` table:
  - `property_invitation_id` (primary key)
  - Address fields: `address_line1`, `address_line2`, `city`, `region`, `postal_code`, `country_code`
  - `invited_by` (foreign key to agents table)
  - `created_at`, `updated_at`, `deleted_at`
- ✅ Added `ClientInvitationsProperties` table:
  - `client_invitation_property_id` (primary key)
  - `client_invitation_id` (foreign key to ClientInvitations)
  - `property_invitation_id` (foreign key to PropertyInvitations)
  - `created_at`, `updated_at`, `deleted_at`

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
- 🔄 **Duplicate Invitations**: If client has existing pending invitation, invalidate previous invite and create new one
- 🔄 **Agent Accidentally Invites Same Client in Subsequent Requests**: Handle when agent sends multiple invitations to same client over time
- Client already has Firebase account with different email
- Invitation expires before client registers
- Client tries to register without invitation
- Multiple agents try to invite same client simultaneously
- **Re-invite Scenarios**:
  - ✅ **Email Delivery Failure**: Re-invite with same details but new token when original email bounces
  - ✅ **Client Detail Corrections**: Re-invite when agent discovers typos in client information
  - ✅ **Email Change Requests**: Re-invite to different email address before client accepts
  - ✅ **Expired Invitations**: Re-invite with extended expiry when client attempts to use expired token
  - ✅ **Multiple Re-invites**: Handle multiple re-invite requests for same client invitation
  - ✅ **Re-invite After Acceptance**: Prevent re-invite of already accepted invitations
  - ✅ **Wrong Agent Re-invite**: Prevent agents from re-inviting clients they didn't originally invite
  - ✅ **Deleted Invitations**: Prevent re-invite of soft-deleted invitation records

## Implementation Details

### Phase 3 Components Added
- ✅ **DTOs**: `ResendInvitationCommand`, `ClientInvitationUpdateRequest`, `ResendInvitationCommandResponse`
- ✅ **Service Methods**: `ResendInvitationAsync` in `IInvitationService` and `InvitationService`
- ✅ **User Service**: `GetUserByEmailAsync` method for existing user detection
- ✅ **Controller**: PUT endpoint `/api/invitations/v1/resend` with authentication and rate limiting
- ✅ **Validation**: `ResendInvitationCommandValidator` and `ClientInvitationUpdateRequestValidator`
- ✅ **Unit Tests**: Comprehensive test coverage with 6 test scenarios including success, error, and edge cases
- ✅ **Security**: Agent ownership validation, accepted invitation prevention, proper error handling