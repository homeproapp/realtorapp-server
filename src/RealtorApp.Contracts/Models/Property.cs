using System;
using System.Collections.Generic;

namespace RealtorApp.Contracts.Models;

public partial class Property
{
    public long PropertyId { get; set; }

    public string? ExternalId { get; set; }

    public string? ExternalSource { get; set; }

    public string? Title { get; set; }

    public short? PropertyType { get; set; }

    public short? Status { get; set; }

    public string AddressLine1 { get; set; } = null!;

    public string? AddressLine2 { get; set; }

    public string City { get; set; } = null!;

    public string Region { get; set; } = null!;

    public string PostalCode { get; set; } = null!;

    public string CountryCode { get; set; } = null!;

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

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

    public virtual ICollection<ClientsProperty> ClientsProperties { get; set; } = new List<ClientsProperty>();

    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();
}
