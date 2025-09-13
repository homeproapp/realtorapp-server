using System;
using System.Collections.Generic;

namespace RealtorApp.Domain.Models;

public partial class Task
{
    public long TaskId { get; set; }

    public long PropertyId { get; set; }

    public string? Title { get; set; }

    public string? Room { get; set; }

    public short? Priority { get; set; }

    public short? Status { get; set; }

    public DateTime? FollowUpDate { get; set; }

    public int? EstimatedCost { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<FilesTask> FilesTasks { get; set; } = new List<FilesTask>();

    public virtual ICollection<Link> Links { get; set; } = new List<Link>();

    public virtual Property Property { get; set; } = null!;

    public virtual ICollection<TaskAttachment> TaskAttachments { get; set; } = new List<TaskAttachment>();
}
