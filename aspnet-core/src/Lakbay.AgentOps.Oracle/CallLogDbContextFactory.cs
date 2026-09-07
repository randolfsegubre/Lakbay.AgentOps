using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lakbay.AgentOps.Oracle;

/// <summary>
/// Design-time factory so `dotnet ef migrations add` can build this context without a
/// live Oracle connection - the connection string here is a placeholder used only to
/// generate migration SQL, never a real credential (the real one lives in user-secrets,
/// see this repo's README/07_MANUAL_SETUP_GUIDE.md).
/// </summary>
public class CallLogDbContextFactory : IDesignTimeDbContextFactory<CallLogDbContext>
{
    public CallLogDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CallLogDbContext>();
        optionsBuilder.UseOracle("User Id=agentops;Password=placeholder-for-migrations-only;Data Source=localhost:1521/FREEPDB1");
        return new CallLogDbContext(optionsBuilder.Options);
    }
}
