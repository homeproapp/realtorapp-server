using System;
using System.Collections.Generic;

namespace RealtorApp.Domain.Models;

public partial class FilesTask
{
    public long FileTaskId { get; set; }

    public long FileId { get; set; }

    public long TaskId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual File File { get; set; } = null!;

    public virtual Task Task { get; set; } = null!;
}
