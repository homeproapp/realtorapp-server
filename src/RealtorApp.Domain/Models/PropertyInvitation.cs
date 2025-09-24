using System;
using System.Collections.Generic;

namespace RealtorApp.Domain.Models;

public partial class PropertyInvitation
{
    public long PropertyInvitationId { get; set; }

    public string AddressLine1 { get; set; } = null!;

    public string? AddressLine2 { get; set; }

    public string City { get; set; } = null!;

    public string Region { get; set; } = null!;

    public string PostalCode { get; set; } = null!;

    public string CountryCode { get; set; } = null!;

    public long InvitedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<ClientInvitationsProperty> ClientInvitationsProperties { get; set; } = new List<ClientInvitationsProperty>();

    public virtual Agent InvitedByNavigation { get; set; } = null!;
}
