using Xunit;

namespace Lakbay.AgentOps.EntityFrameworkCore;

[CollectionDefinition(AgentOpsTestConsts.CollectionDefinitionName)]
public class AgentOpsEntityFrameworkCoreCollection : ICollectionFixture<AgentOpsEntityFrameworkCoreFixture>
{

}
