using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using Lakbay.AgentOps.AgentOffers;
using Lakbay.AgentOps.CallLogging;
using Lakbay.AgentOps.EntityFrameworkCore;
using Lakbay.AgentOps.Hubs;
using Lakbay.AgentOps.MultiTenancy;
using Lakbay.AgentOps.Oracle;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite.Bundling;
using Microsoft.OpenApi;
using OpenIddict.Validation.AspNetCore;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.Account.Web;
using Volo.Abp.AspNetCore.MultiTenancy;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.Security.Claims;
using Volo.Abp.Swashbuckle;
using Volo.Abp.UI.Navigation.Urls;
using Volo.Abp.VirtualFileSystem;

namespace Lakbay.AgentOps;

[DependsOn(
    typeof(AgentOpsHttpApiModule),
    typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreMultiTenancyModule),
    typeof(AgentOpsApplicationModule),
    typeof(AgentOpsEntityFrameworkCoreModule),
    typeof(AbpAspNetCoreMvcUiLeptonXLiteThemeModule),
    typeof(AbpAccountWebOpenIddictModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpSwashbuckleModule),
    typeof(AbpCachingStackExchangeRedisModule)
)]
public class AgentOpsHttpApiHostModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        PreConfigure<OpenIddictBuilder>(builder =>
        {
            builder.AddValidation(options =>
            {
                options.AddAudiences("AgentOps");
                options.UseLocalServer();
                options.UseAspNetCore();
            });
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var hostingEnvironment = context.Services.GetHostingEnvironment();

        ConfigureAuthentication(context);
        ConfigureBundles();
        ConfigureUrls(configuration);
        ConfigureConventionalControllers();
        ConfigureVirtualFileSystem(context);
        ConfigureCors(context, configuration);
        ConfigureSwaggerServices(context, configuration);
        ConfigureRedisCache(context, configuration);
        ConfigureAvailabilityApiClient(context, configuration);
        ConfigureOracleCallLog(context, configuration);
        ConfigureHangfire(context, configuration);
        ConfigureSignalR(context);
    }

    private void ConfigureRedisCache(ServiceConfigurationContext context, IConfiguration configuration)
    {
        Configure<AbpDistributedCacheOptions>(options =>
        {
            options.KeyPrefix = "Lakbay.AgentOps:";
        });

        context.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration["Redis:Configuration"];
        });
    }

    private static void ConfigureAvailabilityApiClient(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddHttpClient<IAvailabilityApiClient, AvailabilityApiClient>(client =>
        {
            var baseUrl = configuration["AvailabilityApi:BaseUrl"]
                ?? throw new InvalidOperationException("AvailabilityApi:BaseUrl is not configured.");
            client.BaseAddress = new Uri(baseUrl);
        });

        // Named client for the BFF proxy in AgentDesktopController (ADR-0021).
        context.Services.AddHttpClient(nameof(Controllers.AgentDesktopController), client =>
        {
            var baseUrl = configuration["Booking:BaseUrl"]
                ?? throw new InvalidOperationException("Booking:BaseUrl is not configured.");
            client.BaseAddress = new Uri(baseUrl);
        });
    }

    private static void ConfigureOracleCallLog(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddDbContext<CallLogDbContext>(options =>
        {
            var connectionString = configuration["Oracle:ConnectionString"];
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                // Real local Oracle install is a manual, elevated one-time step (ADR-0024) -
                // see Lakbay.AgentOps/README.md. Failing loudly here beats a confusing
                // downstream EF exception with no context.
                throw new InvalidOperationException(
                    "Oracle:ConnectionString is not configured. Set it via `dotnet user-secrets set " +
                    "\"Oracle:ConnectionString\" \"...\"` after running .tools/oracle/install-oracle-elevated.ps1 " +
                    "(see Lakbay.AgentOps/README.md).");
            }

            options.UseOracle(connectionString);
        });

        context.Services.AddScoped<ICallRecordRepository, CallRecordRepository>();
    }

    private static void ConfigureHangfire(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(configuration.GetConnectionString("HangfireStorage"), new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.Zero,
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true,
            }));

        context.Services.AddHangfireServer();
        context.Services.AddScoped<ICallRecordJobs, CallRecordJobs>();
    }

    private static void ConfigureSignalR(ServiceConfigurationContext context)
    {
        context.Services.AddSignalR();
        context.Services.AddScoped<IAgentAvailabilityNotifier, AgentAvailabilityNotifier>();
    }

    private void ConfigureAuthentication(ServiceConfigurationContext context)
    {
        context.Services.ForwardIdentityAuthenticationForBearer(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        context.Services.Configure<AbpClaimsPrincipalFactoryOptions>(options =>
        {
            options.IsDynamicClaimsEnabled = true;
        });
    }

    private void ConfigureBundles()
    {
        Configure<AbpBundlingOptions>(options =>
        {
            options.StyleBundles.Configure(
                LeptonXLiteThemeBundles.Styles.Global,
                bundle =>
                {
                    bundle.AddFiles("/global-styles.css");
                }
            );
        });
    }

    private void ConfigureUrls(IConfiguration configuration)
    {
        Configure<AppUrlOptions>(options =>
        {
            options.Applications["MVC"].RootUrl = configuration["App:SelfUrl"];
            options.RedirectAllowedUrls.AddRange(configuration["App:RedirectAllowedUrls"]?.Split(',') ?? Array.Empty<string>());

            options.Applications["Angular"].RootUrl = configuration["App:ClientUrl"];
            options.Applications["Angular"].Urls[AccountUrlNames.PasswordReset] = "account/reset-password";
        });
    }

    private void ConfigureVirtualFileSystem(ServiceConfigurationContext context)
    {
        var hostingEnvironment = context.Services.GetHostingEnvironment();

        if (hostingEnvironment.IsDevelopment())
        {
            Configure<AbpVirtualFileSystemOptions>(options =>
            {
                options.FileSets.ReplaceEmbeddedByPhysical<AgentOpsDomainSharedModule>(
                    Path.Combine(hostingEnvironment.ContentRootPath,
                        $"..{Path.DirectorySeparatorChar}Lakbay.AgentOps.Domain.Shared"));
                options.FileSets.ReplaceEmbeddedByPhysical<AgentOpsDomainModule>(
                    Path.Combine(hostingEnvironment.ContentRootPath,
                        $"..{Path.DirectorySeparatorChar}Lakbay.AgentOps.Domain"));
                options.FileSets.ReplaceEmbeddedByPhysical<AgentOpsApplicationContractsModule>(
                    Path.Combine(hostingEnvironment.ContentRootPath,
                        $"..{Path.DirectorySeparatorChar}Lakbay.AgentOps.Application.Contracts"));
                options.FileSets.ReplaceEmbeddedByPhysical<AgentOpsApplicationModule>(
                    Path.Combine(hostingEnvironment.ContentRootPath,
                        $"..{Path.DirectorySeparatorChar}Lakbay.AgentOps.Application"));
            });
        }
    }

    private void ConfigureConventionalControllers()
    {
        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(typeof(AgentOpsApplicationModule).Assembly);
        });
    }

    private static void ConfigureSwaggerServices(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddAbpSwaggerGenWithOAuth(
            configuration["AuthServer:Authority"]!,
            new Dictionary<string, string>
            {
                    {"AgentOps", "AgentOps API"}
            },
            options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "AgentOps API", Version = "v1" });
                options.DocInclusionPredicate((docName, description) => true);
                options.CustomSchemaIds(type => type.FullName);
            });
    }

    private void ConfigureCors(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder
                    .WithOrigins(configuration["App:CorsOrigins"]?
                        .Split(",", StringSplitOptions.RemoveEmptyEntries)
                        .Select(o => o.RemovePostFix("/"))
                        .ToArray() ?? Array.Empty<string>())
                    .WithAbpExposedHeaders()
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        var env = context.GetEnvironment();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseAbpRequestLocalization();

        if (!env.IsDevelopment())
        {
            app.UseErrorPage();
        }

        app.UseCorrelationId();
        app.MapAbpStaticAssets();
        app.UseRouting();
        app.UseCors();
        app.UseAuthentication();
        app.UseAbpOpenIddictValidation();

        if (MultiTenancyConsts.IsEnabled)
        {
            app.UseMultiTenancy();
        }
        app.UseUnitOfWork();
        app.UseDynamicClaims();
        app.UseAuthorization();

        app.UseSwagger();
        app.UseAbpSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "AgentOps API");

            var configuration = context.ServiceProvider.GetRequiredService<IConfiguration>();
            c.OAuthClientId(configuration["AuthServer:SwaggerClientId"]);
            c.OAuthScopes("AgentOps");
        });

        app.UseAuditing();
        app.UseAbpSerilogEnrichers();

        // Auth-gated per ADR-0025 - never open to anyone, even on a local dev instance.
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = new[] { new RequireAuthenticatedUserDashboardFilter() },
        });

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<AgentAvailabilityHub>("/hubs/agent-availability");
        });

        app.UseConfiguredEndpoints();
    }
}

/// <summary>Blocks anonymous access to the Hangfire dashboard - see ADR-0025.</summary>
public class RequireAuthenticatedUserDashboardFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = ((AspNetCoreDashboardContext)context).HttpContext;
        return httpContext.User?.Identity?.IsAuthenticated == true;
    }
}
