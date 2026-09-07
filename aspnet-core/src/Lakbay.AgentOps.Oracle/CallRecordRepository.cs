using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lakbay.AgentOps.CallLogging;
using Microsoft.EntityFrameworkCore;

namespace Lakbay.AgentOps.Oracle;

public class CallRecordRepository : ICallRecordRepository
{
    private readonly CallLogDbContext _dbContext;

    public CallRecordRepository(CallLogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task InsertAsync(CallRecord record, CancellationToken cancellationToken = default)
    {
        await _dbContext.CallRecords.AddAsync(record, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<CallRecord?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.CallRecords.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(CallRecord record, CancellationToken cancellationToken = default)
    {
        _dbContext.CallRecords.Update(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AgentCallStats>> GetStatsByAgentAsync(DateTime sinceUtc, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CallRecords
            .Where(c => c.CallStartUtc >= sinceUtc)
            .GroupBy(c => c.AgentId)
            .Select(g => new AgentCallStats(
                g.Key,
                g.Count(),
                g.Count(c => c.Outcome == CallOutcome.Booked)))
            .ToListAsync(cancellationToken);
    }
}
