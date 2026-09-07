using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Lakbay.AgentOps.Hubs;

/// <summary>
/// Pushes live availability changes to every connected Lakbay.AgentDesktop client
/// (ADR-0025) - the first real implementation of the live-push mechanism ADR-0008
/// designed for Lakbay.Web but never built. Same event stream
/// Lakbay.AvailabilityApi.Sync already produces; this hub is a second, independent
/// consumer, not a replacement for that design.
/// </summary>
public class AgentAvailabilityHub : Hub
{
    /// <summary>
    /// Agents subscribe per destination they're actively viewing, not to every event
    /// platform-wide - mirrors the per-listing subscription model ADR-0008 already
    /// specifies for Lakbay.Web.
    /// </summary>
    public async Task SubscribeToDestination(string destinationCode)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(destinationCode));
    }

    public async Task UnsubscribeFromDestination(string destinationCode)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(destinationCode));
    }

    public static string GroupName(string destinationCode) => $"destination:{destinationCode}";
}

/// <summary>Server-side helper for pushing to a destination's group from outside the hub (e.g. a Service Bus consumer).</summary>
public interface IAgentAvailabilityNotifier
{
    Task NotifyAvailabilityChangedAsync(string destinationCode, object payload);
}

public class AgentAvailabilityNotifier : IAgentAvailabilityNotifier
{
    private readonly IHubContext<AgentAvailabilityHub> _hubContext;

    public AgentAvailabilityNotifier(IHubContext<AgentAvailabilityHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyAvailabilityChangedAsync(string destinationCode, object payload)
    {
        return _hubContext.Clients
            .Group(AgentAvailabilityHub.GroupName(destinationCode))
            .SendAsync("AvailabilityChanged", destinationCode, payload);
    }
}
