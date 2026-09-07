using System;
using System.Collections.Generic;
using System.Text;
using Lakbay.AgentOps.Localization;
using Volo.Abp.Application.Services;

namespace Lakbay.AgentOps;

/* Inherit your application services from this class.
 */
public abstract class AgentOpsAppService : ApplicationService
{
    protected AgentOpsAppService()
    {
        LocalizationResource = typeof(AgentOpsResource);
    }
}
