using Lakbay.AgentOps.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace Lakbay.AgentOps.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class AgentOpsController : AbpControllerBase
{
    protected AgentOpsController()
    {
        LocalizationResource = typeof(AgentOpsResource);
    }
}
