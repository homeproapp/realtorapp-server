using System;
using System.Collections.Generic;

namespace RealtorApp.Infra.Data;

public partial class Attachment
{
    public long AttachmentId { get; set; }

    public long MessageId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ContactAttachment? ContactAttachment { get; set; }

    public virtual Message Message { get; set; } = null!;

    public virtual TaskAttachment? TaskAttachment { get; set; }
}
