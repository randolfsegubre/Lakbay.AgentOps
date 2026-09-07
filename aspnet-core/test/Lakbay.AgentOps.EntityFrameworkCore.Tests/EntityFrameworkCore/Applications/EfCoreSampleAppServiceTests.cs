using Lakbay.AgentOps.Samples;
using Xunit;

namespace Lakbay.AgentOps.EntityFrameworkCore.Applications;

[Collection(AgentOpsTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<AgentOpsEntityFrameworkCoreTestModule>
{

}
