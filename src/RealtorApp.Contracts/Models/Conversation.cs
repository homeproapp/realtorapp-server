using System;
using System.Collections.Generic;

namespace RealtorApp.Contracts.Models;

public partial class Conversation
{
    public long ConversationId { get; set; }

    public long AgentId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual Agent Agent { get; set; } = null!;

    public virtual ICollection<ClientsConversation> ClientsConversations { get; set; } = new List<ClientsConversation>();

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}
