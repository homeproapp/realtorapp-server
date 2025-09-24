﻿using System;
using System.Collections.Generic;

namespace RealtorApp.Domain.Models;

public partial class ClientInvitation
{
    public long ClientInvitationId { get; set; }

    public string ClientEmail { get; set; } = null!;

    public string? ClientFirstName { get; set; }

    public string? ClientLastName { get; set; }

    public string? ClientPhone { get; set; }

    public Guid InvitationToken { get; set; }

    public long InvitedBy { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? AcceptedAt { get; set; }

    public long? CreatedUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<ClientInvitationsProperty> ClientInvitationsProperties { get; set; } = new List<ClientInvitationsProperty>();

    public virtual User? CreatedUser { get; set; }

    public virtual Agent InvitedByNavigation { get; set; } = null!;
}
