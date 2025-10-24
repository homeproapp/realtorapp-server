using System;
using System.Collections.Generic;

namespace RealtorApp.Domain.Models;

public partial class Reminder
{
    public long ReminderId { get; set; }

    public long UserId { get; set; }

    public string ReminderText { get; set; } = null!;

    public short? ReminderType { get; set; }

    public long ReferencingObjectId { get; set; }

    public DateTime RemindAt { get; set; }

    public bool? IsCompleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
