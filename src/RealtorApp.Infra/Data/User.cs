using System;
using System.Collections.Generic;

namespace RealtorApp.Infra.Data;

public partial class User
{
    public long UserId { get; set; }

    public string Uuid { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string? Phone { get; set; }

    public long? ProfileImageId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual Agent? Agent { get; set; }

    public virtual Client? Client { get; set; }

    public virtual ICollection<MessageRead> MessageReads { get; set; } = new List<MessageRead>();

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual File? ProfileImage { get; set; }

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public virtual ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
}
