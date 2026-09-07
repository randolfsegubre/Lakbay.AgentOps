using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Lakbay.AgentOps.CallLogging;

public class CallRecordJobs : ICallRecordJobs
{
    private readonly ICallRecordRepository _callRecordRepository;
    private readonly ILogger<CallRecordJobs> _logger;

    public CallRecordJobs(ICallRecordRepository callRecordRepository, ILogger<CallRecordJobs> logger)
    {
        _callRecordRepository = callRecordRepository;
        _logger = logger;
    }

    public async Task RecordCallStartAsync(Guid callId, string callerPhoneNumber, string? matchedCustomerName, Guid agentId, DateTime callStartUtc)
    {
        var record = new CallRecord(callId, callerPhoneNumber, matchedCustomerName, agentId, callStartUtc);
        await _callRecordRepository.InsertAsync(record);
    }

    public async Task RecordCallCompletionAsync(Guid callId, CallOutcome outcome, Guid? bookingId, DateTime callEndUtc)
    {
        var record = await _callRecordRepository.FindAsync(callId);
        if (record is null)
        {
            _logger.LogWarning("CompleteCall job ran for {CallId} but no CallRecord exists - the start-call job may still be in flight; Hangfire will retry.", callId);
            throw new InvalidOperationException($"CallRecord {callId} not found yet.");
        }

        record.CompleteCall(outcome, callEndUtc, bookingId);
        await _callRecordRepository.UpdateAsync(record);
    }

    public Task SendFollowUpAsync(Guid callId)
    {
        // Real email/SMS delivery (Twilio, per the original Phase 4 plan in 02_BUILD_PLAN.md)
        // is a follow-up integration, not built here - logging makes the job's real effect
        // visible and verifiable via the Hangfire dashboard without pretending a message
        // was actually sent to a real phone number.
        _logger.LogInformation("Follow-up payment-collection message queued for call {CallId}", callId);
        return Task.CompletedTask;
    }
}
