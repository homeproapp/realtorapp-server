using System;
using System.Collections.Generic;

namespace RealtorApp.Infra.Data;

public partial class Conversation
{
    public long ListingId { get; set; }

    public string? Nickname { get; set; }

    public long? ImageId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual File? Image { get; set; }

    public virtual Listing Listing { get; set; } = null!;

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}
