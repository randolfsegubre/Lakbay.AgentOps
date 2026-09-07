using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Lakbay.AgentOps.Data;

/* This is used if database provider does't define
 * IAgentOpsDbSchemaMigrator implementation.
 */
public class NullAgentOpsDbSchemaMigrator : IAgentOpsDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
