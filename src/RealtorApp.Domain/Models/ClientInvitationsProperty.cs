using System;
using System.Collections.Generic;

namespace RealtorApp.Domain.Models;

public partial class ClientInvitationsProperty
{
    public long ClientInvitationPropertyId { get; set; }

    public long ClientInvitationId { get; set; }

    public long PropertyInvitationId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ClientInvitation ClientInvitation { get; set; } = null!;

    public virtual PropertyInvitation PropertyInvitation { get; set; } = null!;
}
