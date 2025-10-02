using System;
using System.Collections.Generic;

namespace RealtorApp.Domain.Models;

public partial class MessageRead
{
    public long MessageReadId { get; set; }

    public long MessageId { get; set; }

    public long ReaderId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Message Message { get; set; } = null!;

    public virtual User Reader { get; set; } = null!;
}
