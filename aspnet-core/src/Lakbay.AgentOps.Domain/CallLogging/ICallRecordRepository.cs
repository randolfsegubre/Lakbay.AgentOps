using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lakbay.AgentOps.CallLogging;

/// <summary>
/// Deliberately a plain repository interface, not ABP's <c>IRepository&lt;T&gt;</c> -
/// CallRecord lives on Oracle (ADR-0024), a second, independent persistence context
/// from every other entity in this service, which lives on SQL Server via ABP's own
/// EF Core module. Keeping the abstraction plain avoids implying the two share
/// infrastructure they deliberately don't.
/// </summary>
public interface ICallRecordRepository
{
    Task InsertAsync(CallRecord record, CancellationToken cancellationToken = default);

    Task<CallRecord?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    Task UpdateAsync(CallRecord record, CancellationToken cancellationToken = default);

    /// <summary>Simple per-agent performance read model: calls handled and booking conversions, per ADR-0024.</summary>
    Task<IReadOnlyList<AgentCallStats>> GetStatsByAgentAsync(DateTime sinceUtc, CancellationToken cancellationToken = default);
}

public record AgentCallStats(Guid AgentId, int TotalCalls, int BookedCalls);
