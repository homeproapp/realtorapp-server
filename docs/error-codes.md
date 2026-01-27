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

## System Error Codes

### SYS_E001
**Description**: Unhandled exception occurred
**Details**: An unexpected error occurred while processing the request. The full exception details are logged server-side for debugging.
**User Message**: "An unexpected error occurred"
**HTTP Status**: 500

---

## Settings Error Codes

### SETTINGS_E001
**Description**: User not found during profile update
**Details**: The user ID from the JWT token does not exist in the Users table or has been soft-deleted
**User Message**: "Update failed"
**HTTP Status**: 400

### SETTINGS_E002
**Description**: Database exception during profile update
**Details**: An unexpected database error occurred while updating profile fields
**User Message**: "Update failed"
**HTTP Status**: 400

### SETTINGS_E003
**Description**: User not found during email update
**Details**: The user ID from the JWT token does not exist in the Users table or has been soft-deleted
**User Message**: "Update failed"
**HTTP Status**: 400

### SETTINGS_E004
**Description**: Password verification failed for email update
**Details**: The current password provided does not match the user's Firebase password
**User Message**: "Update failed"
**HTTP Status**: 400

### SETTINGS_E005
**Description**: Firebase email update failed
**Details**: Firebase Admin SDK failed to update the user's email (may be due to invalid email, email already in use, etc.)
**User Message**: "Update failed"
**HTTP Status**: 400

### SETTINGS_E006
**Description**: Database email update failed after Firebase success
**Details**: Firebase email was updated but database update failed. Firebase change was rolled back.
**User Message**: "Update failed"
**HTTP Status**: 400

### SETTINGS_E007
**Description**: Database exception during email update with Firebase rollback
**Details**: An exception occurred during database email update. Firebase change was rolled back.
**User Message**: "Update failed"
**HTTP Status**: 400

### SETTINGS_E008
**Description**: Password change failed
**Details**: Firebase Admin SDK failed to change the password (may be due to invalid current password, weak new password, etc.)
**User Message**: "Update failed"
**HTTP Status**: 400

### SETTINGS_E009
**Description**: Avatar upload failed
**Details**: S3 upload or database update for profile image failed
**User Message**: "Upload failed"
**HTTP Status**: 400

### SETTINGS_E010
**Description**: No file provided for avatar upload
**Details**: The request did not include a file attachment
**User Message**: "No file provided"
**HTTP Status**: 400

### SETTINGS_E011
**Description**: Invalid file type for avatar
**Details**: The uploaded file is not an allowed image type (jpeg, png, gif, webp)
**User Message**: "Invalid file type"
**HTTP Status**: 400

### SETTINGS_E012
**Description**: Avatar file too large
**Details**: The uploaded file exceeds the maximum allowed size (5MB)
**User Message**: "File too large"
**HTTP Status**: 400

---

## Usage

When debugging authentication issues:
1. Check the error code in the API response
2. Reference this document to understand the specific issue
3. Investigate the corresponding validation logic in `UserValidationMiddleware`

When debugging system errors:
1. Check the error code in the API response
2. Review server logs for the detailed exception stack trace
3. The logs include the request path, HTTP method, and authenticated user (if applicable)