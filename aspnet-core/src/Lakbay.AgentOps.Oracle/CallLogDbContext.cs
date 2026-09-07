using Lakbay.AgentOps.CallLogging;
using Microsoft.EntityFrameworkCore;

namespace Lakbay.AgentOps.Oracle;

/// <summary>
/// The platform's only Oracle-backed context (ADR-0024) - deliberately not part of
/// ABP's main <c>AgentOpsDbContext</c> (SQL Server), which owns everything else this
/// service persists. Framed as "the call center's existing Oracle CRM," so its scope
/// stays narrow on purpose: call records and the read model derived from them.
/// </summary>
public class CallLogDbContext : DbContext
{
    public DbSet<CallRecord> CallRecords => Set<CallRecord>();

    public CallLogDbContext(DbContextOptions<CallLogDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CallRecord>(entity =>
        {
            // Oracle identifiers are case-insensitive and traditionally uppercase;
            // naming everything explicitly avoids relying on a provider default.
            entity.ToTable("CALL_RECORDS");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Id).HasColumnName("ID");
            entity.Property(c => c.CallerPhoneNumber).HasColumnName("CALLER_PHONE_NUMBER").HasMaxLength(32).IsRequired();
            entity.Property(c => c.MatchedCustomerName).HasColumnName("MATCHED_CUSTOMER_NAME").HasMaxLength(200);
            entity.Property(c => c.AgentId).HasColumnName("AGENT_ID");
            entity.Property(c => c.CallStartUtc).HasColumnName("CALL_START_UTC");
            entity.Property(c => c.CallEndUtc).HasColumnName("CALL_END_UTC");
            entity.Property(c => c.Outcome).HasColumnName("OUTCOME").HasConversion<int>();
            entity.Property(c => c.BookingId).HasColumnName("BOOKING_ID");

            entity.HasIndex(c => c.AgentId);
            entity.HasIndex(c => c.CallStartUtc);
        });
    }
}
