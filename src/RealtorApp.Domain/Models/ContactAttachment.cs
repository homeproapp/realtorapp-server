using System;
using System.Collections.Generic;

namespace RealtorApp.Domain.Models;

public partial class ContactAttachment
{
    public long AttachmentId { get; set; }

    public long ThirdPartyContactId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual Attachment Attachment { get; set; } = null!;

    public virtual ThirdPartyContact ThirdPartyContact { get; set; } = null!;
}
