using System;
using System.Collections.Generic;

namespace RealtorApp.Infra.Data;

public partial class Client
{
    public long UserId { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public string? MaritalStatus { get; set; }

    public int? YearlyIncome { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<ClientInvitation> ClientInvitations { get; set; } = new List<ClientInvitation>();

    public virtual ICollection<ClientsListing> ClientsListings { get; set; } = new List<ClientsListing>();

    public virtual User User { get; set; } = null!;
}
