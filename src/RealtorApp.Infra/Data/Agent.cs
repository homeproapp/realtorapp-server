using System;
using System.Collections.Generic;

namespace RealtorApp.Infra.Data;

public partial class Agent
{
    public long UserId { get; set; }

    public long? TeamId { get; set; }

    public bool? EmailValidated { get; set; }

    public bool? IsTeamLead { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<AgentsListing> AgentsListings { get; set; } = new List<AgentsListing>();

    public virtual ICollection<ClientInvitation> ClientInvitations { get; set; } = new List<ClientInvitation>();

    public virtual ICollection<PropertyInvitation> PropertyInvitations { get; set; } = new List<PropertyInvitation>();

    public virtual Team? Team { get; set; }

    public virtual ICollection<ThirdPartyContact> ThirdPartyContacts { get; set; } = new List<ThirdPartyContact>();

    public virtual User User { get; set; } = null!;
}
