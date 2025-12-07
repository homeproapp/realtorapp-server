using System;
using System.Collections.Generic;

namespace RealtorApp.Infra.Data;

public partial class TaskAttachment
{
    public long AttachmentId { get; set; }

    public long TaskId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual Attachment Attachment { get; set; } = null!;

    public virtual Task Task { get; set; } = null!;
}
