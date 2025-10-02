using System;
using System.Collections.Generic;

namespace RealtorApp.Domain.Models;

public partial class FileType
{
    public long FileTypeId { get; set; }

    public string Name { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<File> Files { get; set; } = new List<File>();
}
