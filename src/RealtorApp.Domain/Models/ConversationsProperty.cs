using System;
using System.Collections.Generic;

namespace RealtorApp.Domain.Models;

public partial class ConversationsProperty
{
    public long ClientConversationId { get; set; }

    public long ConversationId { get; set; }

    public long PropertyId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual Conversation Conversation { get; set; } = null!;

    public virtual Property Property { get; set; } = null!;
}
