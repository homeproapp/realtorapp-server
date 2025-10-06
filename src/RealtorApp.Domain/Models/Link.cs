using System;
using System.Collections.Generic;

namespace RealtorApp.Domain.Models;

public partial class Link
{
    public long LinkId { get; set; }

    public long TaskId { get; set; }

    public string Name { get; set; } = null!;

    public string Url { get; set; } = null!;

    public bool? IsReferral { get; set; }

    public int? TimesUsed { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual Task Task { get; set; } = null!;
}
