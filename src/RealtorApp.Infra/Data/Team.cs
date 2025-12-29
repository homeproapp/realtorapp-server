using System;
using System.Collections.Generic;

namespace RealtorApp.Infra.Data;

public partial class Team
{
    public long TeamId { get; set; }

    public string TeamName { get; set; } = null!;

    public string TeamSite { get; set; } = null!;

    public string Realty { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<Agent> Agents { get; set; } = new List<Agent>();
}
