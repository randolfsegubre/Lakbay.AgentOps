using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Lakbay.AgentOps.AgentOffers;
using Lakbay.AgentOps.CallLogging;
using Microsoft.AspNetCore.Mvc;

namespace Lakbay.AgentOps.Controllers;

/// <summary>
/// The exact, plain REST contract Lakbay.AgentDesktop is built against (documented in
/// ADR-0021 and this repo's README) - deliberately separate from ABP's own conventional
/// auto-API-controller routes (which live under /api/app/... and serve Swagger/other
/// tooling), so the desktop client's contract stays stable and simple regardless of how
/// the ABP application-service layer underneath is organized.
/// </summary>
[ApiController]
[Route("api")]
public class AgentDesktopController : ControllerBase
{
    private readonly IAgentOfferAppService _agentOfferAppService;
    private readonly ICallLogAppService _callLogAppService;
    private readonly IHttpClientFactory _httpClientFactory;

    public AgentDesktopController(
        IAgentOfferAppService agentOfferAppService,
        ICallLogAppService callLogAppService,
        IHttpClientFactory httpClientFactory)
    {
        _agentOfferAppService = agentOfferAppService;
        _callLogAppService = callLogAppService;
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("agent-offer/{destinationCode}")]
    public async Task<ActionResult<AgentDesktopOfferResponse>> GetOffer(string destinationCode)
    {
        var offer = await _agentOfferAppService.GetOfferAsync(destinationCode);

        return Ok(new AgentDesktopOfferResponse
        {
            DestinationName = offer.DestinationName,
            Accommodations = offer.Accommodations.ConvertAll(a => new AgentDesktopAccommodation
            {
                Id = a.Id,
                Name = a.Name,
                Type = a.Type,
                OfficialRating = a.OfficialRating,
            }),
            Packages = offer.Packages.ConvertAll(p => new AgentDesktopPackage
            {
                Id = p.Id,
                Name = p.Name,
                // Named "Price" (not "FromPricePhp") to match Lakbay.AgentDesktop's
                // PackageDto exactly (ADR-0021's documented contract) - the lowest price
                // band is what an agent quotes as the package's starting price.
                Price = p.FromPricePhp ?? 0m,
            }),
            Activities = offer.Activities.ConvertAll(a => new AgentDesktopActivity
            {
                Id = a.Id,
                Name = a.Name,
                Price = a.PricePhp,
            }),
        });
    }

    [HttpPost("bookings/confirm")]
    public async Task<ActionResult<AgentDesktopBookingResult>> ConfirmBooking(
        [FromBody] AgentDesktopConfirmBookingRequest request,
        CancellationToken cancellationToken)
    {
        // BFF pattern (ADR-0021): the desktop app never calls Lakbay.Booking directly -
        // AgentOps proxies it, which is also where a failed attempt gets retried
        // (ADR-0025) instead of the agent having to hang up and call back.
        var bookingClient = _httpClientFactory.CreateClient(nameof(AgentDesktopController));

        using var response = await bookingClient.PostAsJsonAsync(
            "api/bookings/confirm",
            new { request.ProductId, request.DateSlot, request.CustomerId, Channel = request.Channel },
            cancellationToken);

        var result = response.StatusCode switch
        {
            System.Net.HttpStatusCode.OK => await MapConfirmedAsync(response, cancellationToken),
            System.Net.HttpStatusCode.Conflict => new AgentDesktopBookingResult
            {
                Success = false,
                Message = await ReadMessageAsync(response, cancellationToken) ?? "This slot is no longer available.",
            },
            System.Net.HttpStatusCode.NotImplemented => new AgentDesktopBookingResult
            {
                Success = false,
                Message = await ReadMessageAsync(response, cancellationToken) ?? "Online payment is not available yet.",
            },
            _ => new AgentDesktopBookingResult
            {
                Success = false,
                Message = $"Lakbay.Booking returned an unexpected status ({(int)response.StatusCode}).",
            },
        };

        await _callLogAppService.CompleteCallAsync(new CompleteCallDto
        {
            // A real call id would come from the desktop app's active IncomingCallViewModel;
            // this endpoint doesn't yet carry one end-to-end (see README known gaps) - using
            // a fresh id keeps the call-logging path exercised without inventing a fake
            // correlation to a call that doesn't exist yet.
            CallId = Guid.NewGuid(),
            Outcome = result.Success ? CallOutcome.Booked : CallOutcome.NoSale,
            BookingId = result.Success && Guid.TryParse(result.BookingId, out var bookingId) ? bookingId : null,
        });

        return Ok(result);
    }

    private static async Task<AgentDesktopBookingResult> MapConfirmedAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var payload = await response.Content.ReadFromJsonAsync<BookingConfirmedPayload>(cancellationToken: cancellationToken);
        return new AgentDesktopBookingResult
        {
            Success = true,
            BookingId = payload?.BookingId,
            PaymentStatus = payload?.PaymentStatus,
            Message = payload?.Message ?? "Booking confirmed.",
        };
    }

    private static async Task<string?> ReadMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var payload = await response.Content.ReadFromJsonAsync<MessagePayload>(cancellationToken: cancellationToken);
        return payload?.Message;
    }

    private class BookingConfirmedPayload
    {
        public string? BookingId { get; set; }
        public string? PaymentStatus { get; set; }
        public string? Message { get; set; }
    }

    private class MessagePayload
    {
        public string? Message { get; set; }
    }
}

public class AgentDesktopOfferResponse
{
    public string DestinationName { get; set; } = string.Empty;
    public System.Collections.Generic.List<AgentDesktopAccommodation> Accommodations { get; set; } = new();
    public System.Collections.Generic.List<AgentDesktopPackage> Packages { get; set; } = new();
    public System.Collections.Generic.List<AgentDesktopActivity> Activities { get; set; } = new();
}

public class AgentDesktopAccommodation
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public double? OfficialRating { get; set; }
}

public class AgentDesktopPackage
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class AgentDesktopActivity
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class AgentDesktopConfirmBookingRequest
{
    public string ProductId { get; set; } = string.Empty;
    public DateOnly DateSlot { get; set; }
    public string? CustomerId { get; set; }
    public string Channel { get; set; } = "Agent";
}

public class AgentDesktopBookingResult
{
    public bool Success { get; set; }
    public string? BookingId { get; set; }
    public string? PaymentStatus { get; set; }
    public string Message { get; set; } = string.Empty;
}
