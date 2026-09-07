using Volo.Abp.Modularity;

namespace Lakbay.AgentOps;

[DependsOn(
    typeof(AgentOpsDomainModule),
    typeof(AgentOpsTestBaseModule)
)]
public class AgentOpsDomainTestModule : AbpModule
{

}
