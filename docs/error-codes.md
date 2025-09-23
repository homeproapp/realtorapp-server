# Error Codes Documentation

This document contains internal error codes for debugging purposes. These codes are returned in API responses but should not be exposed to end users.

## Authentication Error Codes

### AUTH_E001
**Description**: Invalid or missing user UUID in JWT token
**Details**: The `sub` claim in the JWT token is either missing, empty, or not a valid GUID format
**User Message**: "Authentication failed"
**HTTP Status**: 401

### AUTH_E002
**Description**: User not found in database
**Details**: The user UUID from the JWT token does not exist in the Users table
**User Message**: "Authentication failed"
**HTTP Status**: 401

### AUTH_E003
**Description**: Invalid role in JWT token
**Details**: The `role` claim in the JWT token is either missing, empty, or not one of the valid values (agent/client)
**User Message**: "Authentication failed"
**HTTP Status**: 401

### AUTH_E004
**Description**: Invalid or expired refresh token
**Details**: The refresh token is invalid, expired, or has been revoked
**User Message**: "Authentication failed"
**HTTP Status**: 401

### AUTH_E007
**Description**: User UUID not found in JWT claims during logout-all
**Details**: The `sub` claim is missing or invalid in the authenticated user's JWT token
**User Message**: "Authentication failed"
**HTTP Status**: 401

### AUTH_E008
**Description**: User not found during logout-all
**Details**: The user UUID from JWT claims does not exist in the database
**User Message**: "Authentication failed"
**HTTP Status**: 401

### AUTH_E009
**Description**: Invalid Firebase token during login
**Details**: The Firebase ID token provided is invalid, expired, or malformed
**User Message**: "Authentication failed"
**HTTP Status**: 401

---

## Usage

When debugging authentication issues:
1. Check the error code in the API response
2. Reference this document to understand the specific issue
3. Investigate the corresponding validation logic in `UserValidationMiddleware`