using Lakbay.AgentOps.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace Lakbay.AgentOps.Permissions;

public class AgentOpsPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(AgentOpsPermissions.GroupName);
        //Define your own permissions here. Example:
        //myGroup.AddPermission(AgentOpsPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<AgentOpsResource>(name);
    }
}
