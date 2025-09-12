using System;
using System.Collections.Generic;

namespace RealtorApp.Contracts.Models;

public partial class ClientsConversation
{
    public long ClientConversationId { get; set; }

    public long ConversationId { get; set; }

    public long ClientId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual Client Client { get; set; } = null!;

    public virtual Conversation Conversation { get; set; } = null!;
}
