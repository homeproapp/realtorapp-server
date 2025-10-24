using RealtorApp.Contracts.Common;

namespace RealtorApp.Contracts.Queries.Reminders.Responses;

public class ReminderListingsQueryResponse : ResponseWithError
{
    public ReminderListingItem[] Listings { get; set; } = [];
}

public class ReminderListingItem
{
    public long ListingId { get; set; }
    public required string Address { get; set; }
}
