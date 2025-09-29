using System;
using System.Collections.Generic;

namespace RealtorApp.Domain.Models;

public partial class ClientsProperty
{
    public long ClientPropertyId { get; set; }

    public long PropertyId { get; set; }

    public long ClientId { get; set; }

    public long AgentId { get; set; }

    public long ConversationId { get; set; }

    public string? ExternalId { get; set; }

    public string? ExternalSource { get; set; }

    public string? Title { get; set; }

    public short? PropertyType { get; set; }

    public short? Bedrooms { get; set; }

    public short? Bathrooms { get; set; }

    public int? SquareFeet { get; set; }

    public short? YearBuilt { get; set; }

    public decimal? ListPrice { get; set; }

    public decimal? SalePrice { get; set; }

    public string? CurrencyCode { get; set; }

    public DateTime? ListedAt { get; set; }

    public DateTime? ClosingAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual Agent Agent { get; set; } = null!;

    public virtual Client Client { get; set; } = null!;

    public virtual Conversation Conversation { get; set; } = null!;

    public virtual Property Property { get; set; } = null!;
}
