using System;
using System.Collections.Generic;

namespace RealtorApp.Domain.Models;

public partial class Agent
{
    public long UserId { get; set; }

    public string? Brokerage { get; set; }

    public string? BrokerageTeam { get; set; }

    public string? TeamWebsite { get; set; }

    public string? TeamLead { get; set; }

    public bool? EmailValidated { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<AgentsListing> AgentsListings { get; set; } = new List<AgentsListing>();

    public virtual ICollection<ClientInvitation> ClientInvitations { get; set; } = new List<ClientInvitation>();

    public virtual ICollection<PropertyInvitation> PropertyInvitations { get; set; } = new List<PropertyInvitation>();

    public virtual ICollection<ThirdPartyContact> ThirdPartyContacts { get; set; } = new List<ThirdPartyContact>();

    public virtual User User { get; set; } = null!;
}
