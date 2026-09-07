using Microsoft.Extensions.Localization;
using Lakbay.AgentOps.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace Lakbay.AgentOps;

[Dependency(ReplaceServices = true)]
public class AgentOpsBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<AgentOpsResource> _localizer;

    public AgentOpsBrandingProvider(IStringLocalizer<AgentOpsResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
