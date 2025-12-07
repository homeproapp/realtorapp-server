using System;
using System.Collections.Generic;

namespace RealtorApp.Infra.Data;

public partial class RefreshToken
{
    public long RefreshTokenId { get; set; }

    public long UserId { get; set; }

    public string TokenHash { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
