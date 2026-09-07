using Lakbay.AgentOps.Samples;
using Xunit;

namespace Lakbay.AgentOps.EntityFrameworkCore.Domains;

[Collection(AgentOpsTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<AgentOpsEntityFrameworkCoreTestModule>
{

}
