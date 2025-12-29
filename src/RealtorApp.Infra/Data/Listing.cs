using System;
using System.Collections.Generic;

namespace RealtorApp.Infra.Data;

public partial class Listing
{
    public long ListingId { get; set; }

    public long PropertyId { get; set; }

    public string? ExternalId { get; set; }

    public string? ExternalSource { get; set; }

    public string? Title { get; set; }

    public short? PropertyType { get; set; }

    public short? Bedrooms { get; set; }

    public short? Bathrooms { get; set; }

    public int? SquareFeet { get; set; }

    public short? YearBuilt { get; set; }

    public decimal? ListPrice { get; set; }

    public decimal? SalePrice { get; set; }

    public string? CurrencyCode { get; set; }

    public DateTime? ListedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<AgentsListing> AgentsListings { get; set; } = new List<AgentsListing>();

    public virtual ICollection<ClientsListing> ClientsListings { get; set; } = new List<ClientsListing>();

    public virtual Conversation? Conversation { get; set; }

    public virtual Property Property { get; set; } = null!;

    public virtual ICollection<PropertyInvitation> PropertyInvitations { get; set; } = new List<PropertyInvitation>();

    public virtual ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();

    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();
}
