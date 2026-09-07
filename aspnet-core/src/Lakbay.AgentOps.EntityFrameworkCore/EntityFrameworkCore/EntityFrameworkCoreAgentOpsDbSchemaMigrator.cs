using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Lakbay.AgentOps.Data;
using Volo.Abp.DependencyInjection;

namespace Lakbay.AgentOps.EntityFrameworkCore;

public class EntityFrameworkCoreAgentOpsDbSchemaMigrator
    : IAgentOpsDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCoreAgentOpsDbSchemaMigrator(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolve the AgentOpsDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<AgentOpsDbContext>()
            .Database
            .MigrateAsync();
    }
}
