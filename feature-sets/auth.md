# Authentication Feature Set

## Overview
This document defines the authentication and authorization requirements for the RealtorApp system.

## Current State
- JWT Bearer authentication is partially configured in Program.cs
- UserAuthService exists with caching for user lookups
- SignalR hub has authorization attributes
- Database has Users, Agents, and Clients tables

## Requirements

### 1. Firebase Token Validation (One-time) ✅ COMPLETE
- **Login Endpoint**: POST `/auth/login` (idempotent) ✅
  - Receive Firebase ID token from client ✅
  - Validate token using Firebase Admin SDK ✅
  - Extract user info (email, uid, display name, etc.) ✅
  - Find existing user OR create new agent user in local database ✅
  - Generate API access + refresh tokens ✅
  - Return both tokens to client ✅

### 2. API Access/Refresh Token Strategy ✅ COMPLETE
- **Access Token**: Short-lived JWT (15-30 minutes) ✅
  - **Standard Claims**: ✅
    - `iss` (issuer) - Your API domain ✅
    - `aud` (audience) - Your API identifier ✅
    - `exp` (expiry) - Token expiration ✅
    - `nbf` (not before) - Token valid from ✅
    - `iat` (issued at) - Token issue time ✅
    - `jti` (JWT ID) - Unique token identifier ✅
  - **Custom Claims**: ✅
    - `sub` (subject) - User UUID (Firebase UID) ✅
    - `role` - User type (agent/client) ✅
  - Used for all API requests after login ✅
  - Validated by JWT middleware ✅

- **Refresh Token**: Long-lived token (days/weeks) ✅
  - **Generation**: Cryptographically secure random token (32+ bytes) ✅
  - **Storage**: Store SHA-256 hash in database, never plain text ✅
  - **Verification**: Hash incoming token and compare with stored hash ✅
  - **Rotation**: Generate new token on each refresh ✅
  - Used to generate new access tokens ✅
  - Can be revoked for logout/security ✅
- **Refresh Endpoint**: POST `/auth/refresh` ✅
  - Accept valid refresh token ✅
  - Generate new access token ✅
  - Optionally rotate refresh token ✅

### 3. JWT Validation Requirements ✅ COMPLETE
- **Signature Validation**: Verify token signed with correct secret/key ✅
- **Expiry Validation**: Ensure token not expired (`exp` claim) ✅
- **Issuer Validation**: Verify `iss` claim matches expected issuer ✅
- **Audience Validation**: Verify `aud` claim matches API identifier ✅
- **Not Before Validation**: Ensure token valid (`nbf` claim) ✅
- **Custom Claims Validation**: ✅
  - User UUID exists and is valid format ✅
  - UUID exists in database (via UserAuthService cache) ✅
  - Role is valid (agent/client) ✅
- **Security Validations**: ✅
  - JWT ID uniqueness (prevent replay attacks) ✅
  - Token blacklist check (for logout/revoked tokens) ✅
  - Rate limiting on token validation failures ⏸️ (deferred)

## Implementation Tasks ✅ COMPLETE
- ✅ Firebase Admin SDK integration (FirebaseAuthProviderService)
- ✅ JWT token generation and validation service (JwtService)
- ✅ Refresh token management service (RefreshTokenService)
- ✅ AuthController with all endpoints (login, refresh, logout, logout-all)
- ✅ JWT middleware configuration with comprehensive validation
- ✅ Custom UserValidationMiddleware for additional security checks
- ✅ Error code documentation system (docs/error-codes.md)
- ✅ BaseController pattern for common controller functionality
- ✅ DTOs for efficient database operations
- ✅ Memory cache optimization with GetOrCreateAsync patterns

## API Endpoints ✅ COMPLETE
- POST `/auth/login` - Exchange Firebase token for API tokens (idempotent: login existing or create new agent) ✅
- POST `/auth/refresh` - Get new access token using refresh token ✅
- POST `/auth/logout` - Revoke refresh token ✅
- POST `/auth/logout-all` - Revoke all refresh tokens for user ✅

## Database Changes ✅ COMPLETE
- Use existing `uuid` column in Users table for Firebase UID
- Create new `RefreshTokens` table (see refresh_tokens_schema.sql):
  - `refresh_token_id` (primary key)
  - `user_id` (foreign key to Users)
  - `token_hash` (hashed refresh token)
  - `expires_at` (expiration timestamp)
  - `created_at`, `updated_at` (audit fields)
  - `revoked_at` (for logout/security)
- Consider adding `last_login_at` to Users table

## Background Services
⏸️ **DEFERRED TO LATER SESSION**
- **Token Cleanup Service**: Weekly background service to delete expired refresh tokens
  - Removes tokens where `expires_at < now() - grace_period`
  - Prevents database growth and improves performance

## Token Hashing Implementation
```csharp
// Generate refresh token
byte[] tokenBytes = RandomNumberGenerator.GetBytes(32);
string refreshToken = Convert.ToBase64String(tokenBytes);

// Hash for storage
string tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

// Verification
string incomingHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(incomingToken)));
bool isValid = tokenHash.Equals(incomingHash, StringComparison.OrdinalIgnoreCase);
```

## Security Considerations
- Firebase token validation happens only once at login
- **Refresh Token Security**:
  - Use cryptographically secure random generation (e.g., `RandomNumberGenerator.GetBytes()`)
  - Store SHA-256 hash only, never plain text tokens
  - Consider adding salt to hash for extra security
  - Implement token rotation on refresh
- **JWT Security**:
  - Use strong signing key (256-bit minimum)
  - Configure appropriate token expiration times
  - Validate all standard JWT claims
- Rate limiting on auth endpoints
- Secure configuration management (User Secrets, environment variables)