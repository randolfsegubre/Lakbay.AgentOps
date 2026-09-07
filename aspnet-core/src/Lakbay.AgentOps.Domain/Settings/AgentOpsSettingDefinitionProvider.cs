using Volo.Abp.Settings;

namespace Lakbay.AgentOps.Settings;

public class AgentOpsSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(AgentOpsSettings.MySetting1));
    }
}
