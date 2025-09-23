using System;
using System.Collections.Generic;

namespace RealtorApp.Domain.Models;

public partial class Notification
{
    public long NotificationId { get; set; }

    public long UserId { get; set; }

    public bool? IsRead { get; set; }

    public string? NotificationText { get; set; }

    public short? NotificationType { get; set; }

    public long? ReferencingObjectId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
