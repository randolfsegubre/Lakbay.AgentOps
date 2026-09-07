using Lakbay.AgentOps.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace Lakbay.AgentOps.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AgentOpsEntityFrameworkCoreModule),
    typeof(AgentOpsApplicationContractsModule)
    )]
public class AgentOpsDbMigratorModule : AbpModule
{
}
