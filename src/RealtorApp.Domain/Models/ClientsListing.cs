using System;
using System.Collections.Generic;

namespace RealtorApp.Domain.Models;

public partial class ClientsListing
{
    public long ClientListingId { get; set; }

    public long ClientId { get; set; }

    public long ListingId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual Client Client { get; set; } = null!;

    public virtual Listing Listing { get; set; } = null!;
}
