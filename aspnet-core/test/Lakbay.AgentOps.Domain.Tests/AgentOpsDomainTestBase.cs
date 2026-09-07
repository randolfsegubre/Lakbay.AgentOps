using Volo.Abp.Modularity;

namespace Lakbay.AgentOps;

/* Inherit from this class for your domain layer tests. */
public abstract class AgentOpsDomainTestBase<TStartupModule> : AgentOpsTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
