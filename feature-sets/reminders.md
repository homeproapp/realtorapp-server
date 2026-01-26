# Reminders Feature Set

## Overview
This document defines the recurring reminders system and reminder execution infrastructure. It extends the existing one-time reminder functionality to support recurring schedules (daily, weekly, monthly, yearly) and implements the background processing needed to actually deliver reminder notifications to users.

## Current State
- ✅ Database schema exists with `reminders` table (one-time reminders only)
- ✅ CRUD operations implemented in `ReminderService`
- ✅ Index on `remind_at` column for efficient scheduling queries
- ✅ Soft delete and completion tracking
- ❌ **No recurrence support** - only single `remind_at` DateTime
- ❌ **No execution system** - reminders are stored but never triggered
- ❌ **No push notifications** - FCM not yet integrated
- ❌ **No device token management** - nowhere to store user FCM tokens

## Requirements

### Part 1: Recurring Reminders

#### Recurrence Patterns to Support
| Pattern | Example | Description |
|---------|---------|-------------|
| None | - | One-time reminder (current behavior) |
| Daily | Every day at 9am | Repeats every day at the same time |
| Weekly | Every Monday at 9am | Repeats on a specific day of the week |
| Monthly (by day) | Every 15th at 9am | Repeats on a specific day of the month |
| Yearly | Every Jan 15 at 9am | Repeats on the same date each year |

#### End Conditions
- **End Date**: Recurrence stops after a specific date
- **Max Occurrences**: Recurrence stops after N notifications sent
- **No End**: Recurrence continues indefinitely (until manually stopped)

#### Edge Cases
- Monthly reminder on 31st: Use last day of month for shorter months
- Leap year handling for yearly reminders

### Part 2: Reminder Execution System

#### Execution Requirements
1. **Poll for due reminders** every 60 seconds
2. **Batch processing** to handle multiple due reminders efficiently
3. **Push notifications** via Firebase Cloud Messaging (FCM)
4. **In-app notifications** via SignalR for connected users
5. **Missed reminder handling** when service was down
6. **Next occurrence calculation** for recurring reminders
7. **Completion marking** after successful notification delivery

#### Notification Channels
| Channel | When Used | Status |
|---------|-----------|--------|
| FCM Push | Always (if device token exists) | To be implemented |
| SignalR | If user is connected | Existing infrastructure |
| Email | Fallback if push fails | Existing (AWS SES) |

---

## Database Schema Changes

### 1. Reminders Table Extensions

```sql
-- Add recurrence columns to existing reminders table
ALTER TABLE reminders
    ADD COLUMN recurrence_type smallint NOT NULL DEFAULT 0,
    ADD COLUMN recurrence_days_of_week smallint NOT NULL DEFAULT 0, -- Bitmask: bit 0=Sun, 1=Mon, ... 6=Sat
    ADD COLUMN recurrence_day_of_month smallint,
    ADD COLUMN recurrence_end_date timestamp with time zone,
    ADD COLUMN recurrence_max_occurrences int,
    ADD COLUMN recurrence_occurrences_sent int NOT NULL DEFAULT 0,
    ADD COLUMN last_sent_at timestamp with time zone;

-- Constraints
ALTER TABLE reminders
    ADD CONSTRAINT chk_recurrence_days_of_week
        CHECK (recurrence_days_of_week >= 0 AND recurrence_days_of_week <= 127), -- Max 0b1111111
    ADD CONSTRAINT chk_recurrence_day_of_month
        CHECK (recurrence_day_of_month IS NULL OR (recurrence_day_of_month >= 1 AND recurrence_day_of_month <= 31));

-- Note on days_of_week bitmask:
-- Bit 0 (1)  = Sunday
-- Bit 1 (2)  = Monday
-- Bit 2 (4)  = Tuesday
-- Bit 3 (8)  = Wednesday
-- Bit 4 (16) = Thursday
-- Bit 5 (32) = Friday
-- Bit 6 (64) = Saturday
-- Example: Monday + Wednesday + Friday = 2 + 8 + 32 = 42

-- Index for finding due reminders efficiently
CREATE INDEX ix_reminders_due
ON reminders (remind_at)
WHERE deleted_at IS NULL
  AND (is_completed IS NULL OR is_completed = false);

-- Index for recurring reminders specifically
CREATE INDEX ix_reminders_recurring_active
ON reminders (remind_at, recurrence_type)
WHERE deleted_at IS NULL
  AND (is_completed IS NULL OR is_completed = false)
  AND recurrence_type > 0;
```

### 2. User Device Tokens Table (New)

```sql
CREATE TABLE user_device_tokens (
    device_token_id bigserial PRIMARY KEY,
    user_id bigint NOT NULL REFERENCES users(user_id) ON DELETE RESTRICT,
    device_token text NOT NULL,
    device_platform smallint NOT NULL, -- 0=iOS, 1=Android, 2=Web
    device_name text, -- Optional: "John's iPhone", "Chrome on Windows"
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    updated_at timestamp with time zone NOT NULL DEFAULT now(),
    last_used_at timestamp with time zone,
    deleted_at timestamp with time zone
);

-- Unique active token (same token can't be registered twice)
CREATE UNIQUE INDEX ux_user_device_tokens_token_active
ON user_device_tokens (device_token)
WHERE deleted_at IS NULL;

-- Find all tokens for a user
CREATE INDEX ix_user_device_tokens_user_id
ON user_device_tokens (user_id)
WHERE deleted_at IS NULL;
```

### 3. Reminder Notifications Log Table (New, Optional)

```sql
-- For tracking delivery status and debugging
CREATE TABLE reminder_notification_logs (
    log_id bigserial PRIMARY KEY,
    reminder_id bigint NOT NULL REFERENCES reminders(reminder_id) ON DELETE RESTRICT,
    user_id bigint NOT NULL REFERENCES users(user_id) ON DELETE RESTRICT,
    channel smallint NOT NULL, -- 0=FCM, 1=SignalR, 2=Email
    status smallint NOT NULL, -- 0=Pending, 1=Sent, 2=Delivered, 3=Failed
    error_message text,
    created_at timestamp with time zone NOT NULL DEFAULT now()
);

CREATE INDEX ix_reminder_notification_logs_reminder_id
ON reminder_notification_logs (reminder_id);
```

---

## New Enums

### RecurrenceType
**File:** `src/RealtorApp.Contracts/Enums/RecurrenceType.cs`

```csharp
namespace RealtorApp.Contracts.Enums;

public enum RecurrenceType
{
    None = 0,
    Daily = 1,
    Weekly = 2,
    MonthlyByDayOfMonth = 3,
    Yearly = 4
}
```

### DevicePlatform
**File:** `src/RealtorApp.Contracts/Enums/DevicePlatform.cs`

```csharp
namespace RealtorApp.Contracts.Enums;

public enum DevicePlatform
{
    iOS = 0,
    Android = 1,
    Web = 2
}
```

---

## Contract Updates

### AddOrUpdateReminderCommand (Updated)
**File:** `src/RealtorApp.Contracts/Commands/Reminders/Requests/AddOrUpdateReminderCommand.cs`

```csharp
public class AddOrUpdateReminderCommand
{
    // Existing fields
    public long? ReminderId { get; set; }
    public required string ReminderText { get; set; }
    public ReminderType ReminderType { get; set; }
    public long ReferencingObjectId { get; set; }
    public DateTime RemindAt { get; set; }
    public long ListingId { get; set; }

    // New recurrence fields
    public RecurrenceType RecurrenceType { get; set; } = RecurrenceType.None;
    public DayOfWeek[] RecurrenceDaysOfWeek { get; set; } = [];  // For weekly (multiple days)
    public int? RecurrenceDayOfMonth { get; set; }               // For monthly (1-31)
    public DateTime? RecurrenceEndDate { get; set; }             // Optional end date
    public int? RecurrenceMaxOccurrences { get; set; }           // Optional max count
}
```

### DaysOfWeek Bitmask Helper
**File:** `src/RealtorApp.Domain/Helpers/DaysOfWeekBitmask.cs`

```csharp
public static class DaysOfWeekBitmask
{
    public static short ToMask(DayOfWeek[] days)
    {
        short mask = 0;
        foreach (var day in days)
        {
            mask |= (short)(1 << (int)day);
        }
        return mask;
    }

    public static DayOfWeek[] FromMask(short mask)
    {
        var days = new List<DayOfWeek>();
        for (int i = 0; i < 7; i++)
        {
            if ((mask & (1 << i)) != 0)
            {
                days.Add((DayOfWeek)i);
            }
        }
        return days.ToArray();
    }

    public static bool HasDay(short mask, DayOfWeek day)
    {
        return (mask & (1 << (int)day)) != 0;
    }
}
```

### RegisterDeviceTokenCommand (New)
**File:** `src/RealtorApp.Contracts/Commands/DeviceTokens/Requests/RegisterDeviceTokenCommand.cs`

```csharp
public class RegisterDeviceTokenCommand
{
    public required string DeviceToken { get; set; }
    public DevicePlatform Platform { get; set; }
    public string? DeviceName { get; set; }
}
```

---

## Service Architecture

### Architecture Decision: BackgroundService in API

**Recommendation:** Implement as a `BackgroundService` within the existing API project.

**Rationale:**
1. **Single Deployment** - Avoids infrastructure complexity of separate worker
2. **Shared Configuration** - Reuses existing DbContext, Firebase setup, settings
3. **Simpler Operations** - One application to monitor, scale, and deploy
4. **Sufficient Scale** - Realtor app reminder volume doesn't require independent scaling

**Future Migration Path:** If reminder processing exceeds 1000+/minute or impacts API latency, extract to separate worker service.

### New Services to Create

| Service | Interface | Purpose |
|---------|-----------|---------|
| `ReminderExecutionService` | `BackgroundService` | Polls and processes due reminders |
| `PushNotificationService` | `IPushNotificationService` | Sends FCM push notifications |
| `DeviceTokenService` | `IDeviceTokenService` | Manages user device tokens |
| `RecurrenceCalculator` | (static helper) | Calculates next occurrence dates |

---

## ReminderExecutionService Design

**File:** `src/RealtorApp.Domain/Services/ReminderExecutionService.cs`

### Processing Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    Every 60 seconds                              │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  Query: SELECT * FROM reminders                                  │
│         WHERE remind_at <= NOW()                                 │
│         AND deleted_at IS NULL                                   │
│         AND (is_completed IS NULL OR is_completed = false)       │
│         ORDER BY remind_at                                       │
│         LIMIT 100                                                │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
              ┌───────────────────────────────┐
              │     For each reminder:        │
              └───────────────────────────────┘
                              │
        ┌─────────────────────┼─────────────────────┐
        ▼                     ▼                     ▼
┌───────────────┐    ┌───────────────┐    ┌───────────────┐
│  Send FCM     │    │ Send SignalR  │    │ Update DB     │
│  Push         │    │ (if online)   │    │               │
└───────────────┘    └───────────────┘    └───────────────┘
                              │
                              ▼
              ┌───────────────────────────────┐
              │  Is Recurring?                │
              └───────────────────────────────┘
                     │              │
                    Yes            No
                     │              │
                     ▼              ▼
        ┌─────────────────┐  ┌─────────────────┐
        │ Calculate next  │  │ Mark completed  │
        │ remind_at       │  │ is_completed=   │
        │ Increment sent  │  │ true            │
        │ counter         │  └─────────────────┘
        └─────────────────┘
                     │
                     ▼
        ┌─────────────────┐
        │ End conditions  │
        │ met?            │
        └─────────────────┘
              │        │
             Yes      No
              │        │
              ▼        ▼
        ┌────────┐  ┌────────┐
        │Complete│  │Update  │
        │reminder│  │remind_ │
        │        │  │at      │
        └────────┘  └────────┘
```

### Key Implementation Details

```csharp
public class ReminderExecutionService(
    IServiceScopeFactory scopeFactory,
    ILogger<ReminderExecutionService> logger,
    IOptions<ReminderExecutionSettings> settings) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<ReminderExecutionService> _logger = logger;
    private readonly ReminderExecutionSettings _settings = settings.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing reminders");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(_settings.PollingIntervalSeconds),
                stoppingToken);
        }
    }

    private async Task ProcessDueRemindersAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RealtorAppDbContext>();
        var pushService = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();
        var hubContext = scope.ServiceProvider
            .GetRequiredService<IHubContext<LiveUpdatesHub>>();

        var now = DateTime.UtcNow;

        // Efficient query using existing index
        var dueReminders = await dbContext.Reminders
            .AsNoTracking()
            .Where(r => r.RemindAt <= now
                && r.DeletedAt == null
                && (r.IsCompleted == null || r.IsCompleted == false))
            .OrderBy(r => r.RemindAt)
            .Take(_settings.BatchSize)
            .ToListAsync(ct);

        foreach (var reminder in dueReminders)
        {
            await ProcessSingleReminderAsync(
                dbContext, pushService, hubContext, reminder, ct);
        }
    }
}
```

---

## Recurrence Calculation

**File:** `src/RealtorApp.Domain/Helpers/RecurrenceCalculator.cs`

```csharp
public static class RecurrenceCalculator
{
    public static DateTime? CalculateNextOccurrence(
        DateTime currentRemindAt,
        RecurrenceType recurrenceType,
        short daysOfWeekMask = 0,
        int? dayOfMonth = null)
    {
        return recurrenceType switch
        {
            RecurrenceType.None => null,
            RecurrenceType.Daily => currentRemindAt.AddDays(1),
            RecurrenceType.Weekly => CalculateNextWeeklyDate(currentRemindAt, daysOfWeekMask),
            RecurrenceType.MonthlyByDayOfMonth =>
                CalculateNextMonthlyDate(currentRemindAt, dayOfMonth),
            RecurrenceType.Yearly => currentRemindAt.AddYears(1),
            _ => null
        };
    }

    private static DateTime CalculateNextWeeklyDate(
        DateTime current, short daysOfWeekMask)
    {
        if (daysOfWeekMask == 0) return current.AddDays(7); // Fallback

        // Find next day in the mask
        for (int i = 1; i <= 7; i++)
        {
            var nextDate = current.AddDays(i);
            var nextDay = (int)nextDate.DayOfWeek;
            if ((daysOfWeekMask & (1 << nextDay)) != 0)
            {
                return new DateTime(
                    nextDate.Year, nextDate.Month, nextDate.Day,
                    current.Hour, current.Minute, current.Second,
                    DateTimeKind.Utc);
            }
        }

        return current.AddDays(7); // Should not reach here
    }

    private static DateTime CalculateNextMonthlyDate(
        DateTime current, int? targetDay)
    {
        var day = targetDay ?? current.Day;
        var nextMonth = current.AddMonths(1);
        var daysInMonth = DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month);
        var actualDay = Math.Min(day, daysInMonth); // Handle 31st in Feb

        return new DateTime(
            nextMonth.Year,
            nextMonth.Month,
            actualDay,
            current.Hour,
            current.Minute,
            current.Second,
            DateTimeKind.Utc);
    }

    public static bool HasReachedEndCondition(
        Reminder reminder,
        DateTime? nextOccurrence)
    {
        // Check max occurrences
        if (reminder.RecurrenceMaxOccurrences.HasValue &&
            reminder.RecurrenceOccurrencesSent >= reminder.RecurrenceMaxOccurrences)
        {
            return true;
        }

        // Check end date
        if (reminder.RecurrenceEndDate.HasValue &&
            nextOccurrence.HasValue &&
            nextOccurrence > reminder.RecurrenceEndDate)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// For missed reminders: Calculate the next future occurrence, skipping past ones.
    /// </summary>
    public static DateTime CalculateNextFutureOccurrence(
        Reminder reminder, DateTime now)
    {
        var next = reminder.RemindAt;

        while (next <= now)
        {
            var calculated = CalculateNextOccurrence(
                next,
                (RecurrenceType)reminder.RecurrenceType,
                reminder.RecurrenceDaysOfWeek,
                reminder.RecurrenceDayOfMonth);

            if (calculated == null) break;
            next = calculated.Value;
        }

        return next;
    }
}
```

---

## FCM Push Notification Integration

### PushNotificationService
**File:** `src/RealtorApp.Domain/Services/PushNotificationService.cs`

```csharp
public class PushNotificationService(
    RealtorAppDbContext dbContext,
    ILogger<PushNotificationService> logger) : IPushNotificationService
{
    private readonly RealtorAppDbContext _dbContext = dbContext;
    private readonly ILogger<PushNotificationService> _logger = logger;

    public async Task<SendResult> SendReminderNotificationAsync(
        long userId,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken ct = default)
    {
        // Get user's device tokens
        var tokens = await _dbContext.UserDeviceTokens
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.DeletedAt == null)
            .Select(t => t.DeviceToken)
            .ToListAsync(ct);

        if (tokens.Count == 0)
        {
            return new SendResult { Success = false, Reason = "No device tokens" };
        }

        var message = new MulticastMessage
        {
            Tokens = tokens,
            Notification = new Notification
            {
                Title = title,
                Body = body
            },
            Data = data
        };

        try
        {
            var response = await FirebaseMessaging.DefaultInstance
                .SendEachForMulticastAsync(message, ct);

            // Handle invalid tokens
            await HandleInvalidTokensAsync(tokens, response, ct);

            return new SendResult
            {
                Success = response.SuccessCount > 0,
                SuccessCount = response.SuccessCount,
                FailureCount = response.FailureCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send FCM notification to user {UserId}", userId);
            return new SendResult { Success = false, Reason = ex.Message };
        }
    }

    private async Task HandleInvalidTokensAsync(
        List<string> tokens,
        BatchResponse response,
        CancellationToken ct)
    {
        // Soft-delete invalid tokens
        for (int i = 0; i < response.Responses.Count; i++)
        {
            if (!response.Responses[i].IsSuccess &&
                IsInvalidTokenError(response.Responses[i].Exception))
            {
                await _dbContext.UserDeviceTokens
                    .Where(t => t.DeviceToken == tokens[i])
                    .ExecuteUpdateAsync(
                        s => s.SetProperty(t => t.DeletedAt, DateTime.UtcNow), ct);
            }
        }
    }
}
```

---

## API Endpoints

### Device Token Management
- **POST** `/api/device-tokens/v1/register` - Register/update device token
- **DELETE** `/api/device-tokens/v1/{tokenId}` - Remove device token (logout)

### Reminder Updates (Existing endpoints, extended)
- **POST** `/api/reminders/v1/reminder` - Create/update with recurrence fields
- **GET** `/api/reminders/v1/{reminderId}` - Returns recurrence info

---

## Configuration

### appsettings.json Additions

```json
{
  "ReminderExecution": {
    "Enabled": true,
    "PollingIntervalSeconds": 60,
    "BatchSize": 100,
    "SendMissedReminders": false,
    "MaxMissedReminderAgeMinutes": 1440
  },
  "Fcm": {
    "Enabled": true,
    "ProjectId": "your-firebase-project-id"
  }
}
```

### Settings Classes

**File:** `src/RealtorApp.Domain/Settings/ReminderExecutionSettings.cs`

```csharp
public class ReminderExecutionSettings
{
    public bool Enabled { get; set; } = true;
    public int PollingIntervalSeconds { get; set; } = 60;
    public int BatchSize { get; set; } = 100;
    public bool SendMissedReminders { get; set; } = false;
    public int MaxMissedReminderAgeMinutes { get; set; } = 1440; // 24 hours
}
```

---

## Service Registration

**File:** `src/RealtorApp.Api/Program.cs`

```csharp
// Configuration
builder.Services.Configure<ReminderExecutionSettings>(
    builder.Configuration.GetSection("ReminderExecution"));

// Services
builder.Services.AddScoped<IPushNotificationService, PushNotificationService>();
builder.Services.AddScoped<IDeviceTokenService, DeviceTokenService>();

// Background service (conditional)
if (builder.Configuration.GetValue<bool>("ReminderExecution:Enabled"))
{
    builder.Services.AddHostedService<ReminderExecutionService>();
}
```

---

## Error Handling Strategy

| Error Type | Handling | Retry |
|------------|----------|-------|
| Transient (network, FCM temp failure) | Log, leave reminder as pending | Next poll (60s) |
| Invalid device token | Soft-delete token, continue | No |
| Database error | Log, rollback, leave pending | Next poll |
| All tokens failed | Log, consider email fallback | Manual |

---

## Missed Reminder Handling

**Strategy: Skip to Next Future Occurrence**

When the service was down and comes back online:

1. **Query finds all overdue reminders** (remind_at <= NOW)
2. **For non-recurring**: Send notification, mark complete
3. **For recurring with missed occurrences**:
   - Use `RecurrenceCalculator.CalculateNextFutureOccurrence()` to jump forward
   - Send ONE notification: "Reminder: {text}" (no special missed messaging)
   - Update `remind_at` to next future occurrence
   - Increment `recurrence_occurrences_sent` by 1 (not by number missed)
   - Continue normal cycle

**Why this approach:**
- Users don't want to be bombarded with missed notifications
- Simple implementation, no "catch-up" complexity
- Respects user's time - they get reminded going forward

**Example:**
- Reminder set for "Every Monday at 9am"
- Service down for 3 weeks (missed 3 Mondays)
- On restart: Send one notification, set next remind_at to upcoming Monday
- `recurrence_occurrences_sent` increments by 1

---

## Critical Files to Modify/Create

### Modify
- `src/RealtorApp.Infra/Data/Reminder.cs` - Add recurrence properties
- `src/RealtorApp.Infra/Data/RealtorAppDbContext.cs` - Add UserDeviceToken DbSet
- `src/RealtorApp.Domain/Services/ReminderService.cs` - Handle recurrence in CRUD
- `src/RealtorApp.Contracts/Commands/Reminders/Requests/AddOrUpdateReminderCommand.cs` - Add recurrence fields
- `src/RealtorApp.Api/Program.cs` - Register new services

### Create
- `src/RealtorApp.Contracts/Enums/RecurrenceType.cs`
- `src/RealtorApp.Contracts/Enums/DevicePlatform.cs`
- `src/RealtorApp.Infra/Data/UserDeviceToken.cs`
- `src/RealtorApp.Infra/Data/ReminderNotificationLog.cs`
- `src/RealtorApp.Domain/Services/ReminderExecutionService.cs`
- `src/RealtorApp.Domain/Services/PushNotificationService.cs`
- `src/RealtorApp.Domain/Services/DeviceTokenService.cs`
- `src/RealtorApp.Domain/Interfaces/IPushNotificationService.cs`
- `src/RealtorApp.Domain/Interfaces/IDeviceTokenService.cs`
- `src/RealtorApp.Domain/Helpers/RecurrenceCalculator.cs`
- `src/RealtorApp.Domain/Helpers/DaysOfWeekBitmask.cs`
- `src/RealtorApp.Domain/Settings/ReminderExecutionSettings.cs`
- `src/RealtorApp.Api/Controllers/DeviceTokensController.cs`
- EF Migration for schema changes

---

## Implementation Phases

### Phase 1: Database & Contracts
1. Create EF migration for reminder recurrence columns
2. Create migration for `user_device_tokens` table
3. Add `RecurrenceType` and `DevicePlatform` enums
4. Update `AddOrUpdateReminderCommand` with recurrence fields
5. Update entity models

### Phase 2: Recurrence Logic
1. Implement `RecurrenceCalculator` helper
2. Update `ReminderService` to handle recurrence in add/update
3. Add validation for recurrence fields
4. Unit tests for recurrence calculation

### Phase 3: Device Token Management
1. Create `UserDeviceToken` entity
2. Implement `DeviceTokenService`
3. Create `DeviceTokensController` with register/remove endpoints
4. Integration tests

### Phase 4: Push Notifications
1. Configure Firebase Admin SDK for FCM
2. Implement `PushNotificationService`
3. Handle invalid token cleanup
4. Integration tests with FCM

### Phase 5: Reminder Execution
1. Implement `ReminderExecutionService` background service
2. Add SignalR integration for online users
3. Add configuration settings
4. End-to-end testing

### Phase 6: Polish & Monitoring
1. Add logging and metrics
2. Implement notification log table (optional)
3. Add health checks for background service
4. Documentation

---

## Verification Plan

### Unit Tests
- [ ] `RecurrenceCalculator.CalculateNextOccurrence` - all recurrence types
- [ ] Weekly multi-day: Mon+Wed+Fri finds next correct day
- [ ] Weekly edge case: Saturday → Monday wrap-around
- [ ] Monthly edge case: 31st in February → 28th/29th
- [ ] End condition checks: max occurrences, end date
- [ ] `DaysOfWeekBitmask.ToMask` and `FromMask` round-trip
- [ ] `ReminderService` recurrence field persistence
- [ ] `CalculateNextFutureOccurrence` skips past dates correctly

### Integration Tests
- [ ] Create recurring reminder via API
- [ ] Device token registration/removal
- [ ] FCM send with mock Firebase
- [ ] Background service processes due reminders
- [ ] Notification log entries created on send

### Manual/E2E Tests
- [ ] Create daily reminder, wait for execution, verify notification received
- [ ] Create weekly reminder on Mon+Wed+Fri, verify fires on correct days
- [ ] Create monthly reminder on 31st, verify Feb handling
- [ ] Stop/start API, verify missed reminders skip to future occurrence
- [ ] Multiple device tokens for same user
- [ ] Invalid token gets soft-deleted after FCM rejection
