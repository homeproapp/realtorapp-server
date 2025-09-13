using System;
using System.Collections.Generic;

namespace RealtorApp.Domain.Models;

public partial class Conversation
{
    public long ConversationId { get; set; }

    public long AgentId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual Agent Agent { get; set; } = null!;

    public virtual ICollection<ConversationsProperty> ConversationsProperties { get; set; } = new List<ConversationsProperty>();

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}
