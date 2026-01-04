using System;
using System.Collections.Generic;

namespace RealtorApp.Infra.Data;

public partial class TeammateInvitation
{
    public long TeammateInvitationId { get; set; }

    public string TeammateEmail { get; set; } = null!;

    public string TeammateFirstName { get; set; } = null!;

    public string TeammateLastName { get; set; } = null!;

    public string? TeammatePhone { get; set; }

    public short TeammateRoleType { get; set; }

    public Guid InvitationToken { get; set; }

    public long InvitedBy { get; set; }

    public long InvitedListingId { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? AcceptedAt { get; set; }

    public long? CreatedUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual User? CreatedUser { get; set; }

    public virtual Agent InvitedByNavigation { get; set; } = null!;

    public virtual Listing InvitedListing { get; set; } = null!;
}
