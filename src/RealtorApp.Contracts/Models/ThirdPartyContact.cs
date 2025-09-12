using System;
using System.Collections.Generic;

namespace RealtorApp.Contracts.Models;

public partial class ThirdPartyContact
{
    public long ThirdPartyContactId { get; set; }

    public long AgentId { get; set; }

    public string? Name { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Trade { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual Agent Agent { get; set; } = null!;

    public virtual ICollection<ContactAttachment> ContactAttachments { get; set; } = new List<ContactAttachment>();
}
