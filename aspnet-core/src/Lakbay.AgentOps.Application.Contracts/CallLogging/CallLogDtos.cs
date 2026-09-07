using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Lakbay.AgentOps.CallLogging;

public class StartCallDto
{
    public Guid CallId { get; set; }
    public string CallerPhoneNumber { get; set; } = string.Empty;
    public string? MatchedCustomerName { get; set; }
    public Guid AgentId { get; set; }
}

public class CompleteCallDto
{
    public Guid CallId { get; set; }
    public CallOutcome Outcome { get; set; }
    public Guid? BookingId { get; set; }
}

public interface ICallLogAppService : IApplicationService
{
    /// <summary>Queues the call-start write as a Hangfire job (ADR-0025) - never blocks the live call.</summary>
    Task StartCallAsync(StartCallDto input);

    Task CompleteCallAsync(CompleteCallDto input);
}
