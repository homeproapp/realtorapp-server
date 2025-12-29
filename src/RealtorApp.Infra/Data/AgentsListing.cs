using System;
using System.Collections.Generic;

namespace RealtorApp.Infra.Data;

public partial class AgentsListing
{
    public long AgentListingId { get; set; }

    public long AgentId { get; set; }

    public long ListingId { get; set; }

    public bool IsLeadAgent { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual Agent Agent { get; set; } = null!;

    public virtual Listing Listing { get; set; } = null!;
}
