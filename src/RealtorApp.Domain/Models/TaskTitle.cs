using System;
using System.Collections.Generic;

namespace RealtorApp.Domain.Models;

public partial class TaskTitle
{
    public long TaskTitleId { get; set; }

    public string TaskTitle1 { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
