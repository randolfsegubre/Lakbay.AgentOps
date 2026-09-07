using Volo.Abp.Modularity;

namespace Lakbay.AgentOps;

public abstract class AgentOpsApplicationTestBase<TStartupModule> : AgentOpsTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
