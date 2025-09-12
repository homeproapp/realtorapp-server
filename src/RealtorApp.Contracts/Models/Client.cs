using System;
using System.Collections.Generic;

namespace RealtorApp.Contracts.Models;

public partial class Client
{
    public long UserId { get; set; }

    public short? Age { get; set; }

    public string? MaritalStatus { get; set; }

    public int? YearlyIncome { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<ClientsConversation> ClientsConversations { get; set; } = new List<ClientsConversation>();

    public virtual ICollection<ClientsProperty> ClientsProperties { get; set; } = new List<ClientsProperty>();

    public virtual User User { get; set; } = null!;
}
