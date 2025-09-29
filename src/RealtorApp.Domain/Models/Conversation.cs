using System;
using System.Collections.Generic;

namespace RealtorApp.Domain.Models;

public partial class Conversation
{
    public long ConversationId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<ClientsProperty> ClientsProperties { get; set; } = new List<ClientsProperty>();

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}
