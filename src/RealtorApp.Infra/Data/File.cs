using System;
using System.Collections.Generic;

namespace RealtorApp.Infra.Data;

public partial class File
{
    public long FileId { get; set; }

    public Guid Uuid { get; set; }

    public string? FileExtension { get; set; }

    public long FileTypeId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();

    public virtual FileType FileType { get; set; } = null!;

    public virtual ICollection<FilesTask> FilesTasks { get; set; } = new List<FilesTask>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
