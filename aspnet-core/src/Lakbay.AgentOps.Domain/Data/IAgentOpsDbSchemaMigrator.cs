using System.Threading.Tasks;

namespace Lakbay.AgentOps.Data;

public interface IAgentOpsDbSchemaMigrator
{
    Task MigrateAsync();
}
