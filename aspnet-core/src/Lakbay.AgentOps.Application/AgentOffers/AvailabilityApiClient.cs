using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Lakbay.AgentOps.AgentOffers;

/// <summary>
/// Real GraphQL client against Lakbay.AvailabilityApi (schema: Lakbay.Contracts/schema/lakbay.graphql).
/// One query per offer lookup - accommodations and activities for a destination, exactly
/// the two collections AgentOfferAppService needs, nothing this service doesn't use.
/// </summary>
public class AvailabilityApiClient : IAvailabilityApiClient
{
    private const string Query = """
        query AgentOffer($destinationId: String!) {
          accommodations(destinationId: $destinationId) {
            id
            name
            type
            officialRating
            destination {
              name
            }
          }
          activities(destinationId: $destinationId) {
            id
            name
            pricePhp
            durationLabel
          }
          products(filter: { destinationId: $destinationId }) {
            id
            name
            priceBands {
              pricePhp
            }
          }
        }
        """;

    private readonly HttpClient _httpClient;

    public AvailabilityApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AgentOfferGraphQlResult> GetOfferDataAsync(string destinationId, CancellationToken cancellationToken = default)
    {
        var request = new GraphQlRequest(Query, new Dictionary<string, object?> { ["destinationId"] = destinationId });

        using var response = await _httpClient.PostAsJsonAsync("graphql", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<GraphQlResponse<AgentOfferGraphQlResult>>(cancellationToken: cancellationToken);
        if (payload?.Data is null)
        {
            throw new InvalidOperationException(
                $"Lakbay.AvailabilityApi returned no data for destination '{destinationId}': " +
                (payload?.Errors is { Count: > 0 }
                    ? string.Join("; ", payload.Errors.ConvertAll(e => e.Message))
                    : "empty response"));
        }

        return payload.Data;
    }

    private record GraphQlRequest(string Query, Dictionary<string, object?> Variables);

    private class GraphQlResponse<T>
    {
        [JsonPropertyName("data")]
        public T? Data { get; set; }

        [JsonPropertyName("errors")]
        public List<GraphQlError>? Errors { get; set; }
    }

    private class GraphQlError
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}

public interface IAvailabilityApiClient
{
    Task<AgentOfferGraphQlResult> GetOfferDataAsync(string destinationId, CancellationToken cancellationToken = default);
}

public class AgentOfferGraphQlResult
{
    [JsonPropertyName("accommodations")]
    public List<AccommodationGraphQlModel> Accommodations { get; set; } = new();

    [JsonPropertyName("activities")]
    public List<ActivityOfferDto> Activities { get; set; } = new();

    [JsonPropertyName("products")]
    public List<ProductGraphQlModel> Products { get; set; } = new();
}

/// <summary>
/// A "Product" in the real Lakbay schema is a bookable holiday package - the only unit
/// Lakbay.Booking's ConfirmBookingCommand actually knows how to book (ProductId +
/// DateSlot). Accommodations/Activities inform the call; Products are what get booked.
/// </summary>
public class ProductGraphQlModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("priceBands")]
    public List<PriceBandGraphQlModel> PriceBands { get; set; } = new();
}

public class PriceBandGraphQlModel
{
    [JsonPropertyName("pricePhp")]
    public decimal PricePhp { get; set; }
}

/// <summary>Raw GraphQL shape - carries the nested Destination the public AccommodationOfferDto
/// deliberately doesn't expose, since AgentOfferDto already names the destination once at the top level.</summary>
public class AccommodationGraphQlModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("officialRating")]
    public double? OfficialRating { get; set; }

    [JsonPropertyName("destination")]
    public DestinationGraphQlModel? Destination { get; set; }
}

public class DestinationGraphQlModel
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
