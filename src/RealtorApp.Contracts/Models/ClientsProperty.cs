using System;
using System.Collections.Generic;

namespace RealtorApp.Contracts.Models;

public partial class ClientsProperty
{
    public long ClientPropertyId { get; set; }

    public long PropertyId { get; set; }

    public long ClientId { get; set; }

    public long AgentId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual Agent Agent { get; set; } = null!;

    public virtual Client Client { get; set; } = null!;

    public virtual Property Property { get; set; } = null!;
}
