using System;
using System.Collections.Generic;

namespace Lakbay.AgentOps.AgentOffers;

/// <summary>
/// The agent-shaped bundle for one destination: everything an agent needs to make an
/// offer during a live call, aggregated from Lakbay.AvailabilityApi (accommodations,
/// activities) in one round trip instead of several. Cached in Redis (ADR-0025).
/// </summary>
public class AgentOfferDto
{
    public string DestinationCode { get; set; } = string.Empty;
    public string DestinationName { get; set; } = string.Empty;
    public List<AccommodationOfferDto> Accommodations { get; set; } = new();
    public List<PackageOfferDto> Packages { get; set; } = new();
    public List<ActivityOfferDto> Activities { get; set; } = new();
    public DateTimeOffset CachedAt { get; set; }
}

/// <summary>
/// A bookable holiday package (Lakbay.Contracts' "Product") - the only unit
/// Lakbay.Booking's ConfirmBookingCommand actually knows how to book. Accommodations
/// and Activities inform the call; a Package's Id is what gets sent to confirm a booking.
/// </summary>
public class PackageOfferDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    /// <summary>Lowest price band found - the "starting from" price an agent quotes on a call.</summary>
    public decimal? FromPricePhp { get; set; }
}

public class AccommodationOfferDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public double? OfficialRating { get; set; }
}

public class ActivityOfferDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal PricePhp { get; set; }
    public string DurationLabel { get; set; } = string.Empty;
}
