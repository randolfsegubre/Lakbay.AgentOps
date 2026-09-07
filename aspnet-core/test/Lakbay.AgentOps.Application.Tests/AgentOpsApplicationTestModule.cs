using Volo.Abp.Modularity;

namespace Lakbay.AgentOps;

[DependsOn(
    typeof(AgentOpsApplicationModule),
    typeof(AgentOpsDomainTestModule)
)]
public class AgentOpsApplicationTestModule : AbpModule
{

}
