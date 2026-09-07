using System;
using Volo.Abp.Domain.Entities;

namespace Lakbay.AgentOps.CallLogging;

/// <summary>
/// One row per agent-handled call. Persisted to Oracle (ADR-0024), deliberately kept
/// separate from every other entity in this service, which lives on SQL Server.
/// </summary>
public class CallRecord : Entity<Guid>
{
    public string CallerPhoneNumber { get; private set; }
    public string? MatchedCustomerName { get; private set; }
    public Guid AgentId { get; private set; }
    public DateTime CallStartUtc { get; private set; }
    public DateTime? CallEndUtc { get; private set; }
    public CallOutcome Outcome { get; private set; }
    public Guid? BookingId { get; private set; }

    private CallRecord()
    {
        CallerPhoneNumber = string.Empty;
    }

    public CallRecord(
        Guid id,
        string callerPhoneNumber,
        string? matchedCustomerName,
        Guid agentId,
        DateTime callStartUtc)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(callerPhoneNumber))
            throw new ArgumentException("Caller phone number is required.", nameof(callerPhoneNumber));

        CallerPhoneNumber = callerPhoneNumber;
        MatchedCustomerName = matchedCustomerName;
        AgentId = agentId;
        CallStartUtc = callStartUtc;
        Outcome = CallOutcome.InProgress;
    }

    public void CompleteCall(CallOutcome outcome, DateTime callEndUtc, Guid? bookingId)
    {
        if (callEndUtc < CallStartUtc)
            throw new ArgumentException("Call cannot end before it started.", nameof(callEndUtc));

        Outcome = outcome;
        CallEndUtc = callEndUtc;
        BookingId = bookingId;
    }
}
