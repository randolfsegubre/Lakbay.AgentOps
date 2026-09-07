using System;
using System.Threading.Tasks;
using Hangfire;
using Volo.Abp.Application.Services;

namespace Lakbay.AgentOps.CallLogging;

/// <summary>
/// Records calls via Hangfire (ADR-0025) so a slow Oracle write (ADR-0024) never blocks
/// the live call - StartCallAsync/CompleteCallAsync return as soon as the job is queued,
/// not once it's actually written.
/// </summary>
public class CallLogAppService : ApplicationService, ICallLogAppService
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public CallLogAppService(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    public Task StartCallAsync(StartCallDto input)
    {
        _backgroundJobClient.Enqueue<ICallRecordJobs>(jobs =>
            jobs.RecordCallStartAsync(input.CallId, input.CallerPhoneNumber, input.MatchedCustomerName, input.AgentId, DateTime.UtcNow));

        return Task.CompletedTask;
    }

    public Task CompleteCallAsync(CompleteCallDto input)
    {
        _backgroundJobClient.Enqueue<ICallRecordJobs>(jobs =>
            jobs.RecordCallCompletionAsync(input.CallId, input.Outcome, input.BookingId, DateTime.UtcNow));

        if (input.Outcome == CallOutcome.Booked || input.Outcome == CallOutcome.FollowUpNeeded)
        {
            // Fire-and-retry, not fire-and-forget (ADR-0025): Hangfire's own retry policy
            // covers a transient failure sending the follow-up rather than silently losing it.
            _backgroundJobClient.Enqueue<ICallRecordJobs>(jobs => jobs.SendFollowUpAsync(input.CallId));
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// The actual Hangfire job bodies, kept in their own interface so they're independently
/// testable and so Hangfire's serializer only ever needs simple, serializable arguments
/// (no DTOs with behavior, no captured closures).
/// </summary>
public interface ICallRecordJobs
{
    Task RecordCallStartAsync(Guid callId, string callerPhoneNumber, string? matchedCustomerName, Guid agentId, DateTime callStartUtc);

    Task RecordCallCompletionAsync(Guid callId, CallOutcome outcome, Guid? bookingId, DateTime callEndUtc);

    Task SendFollowUpAsync(Guid callId);
}
