using System.Reflection;
using EPiServer.Framework.Hosting;
using EPiServer.Web.Hosting;
using Geta.Optimizely.Tags.Infrastructure.Configuration;
using Geta.Optimizely.Tags.Infrastructure.Initialization;
using Geta.Optimizely.Tags.Web.Services;
using Optimizely.Graph.Cms.Configuration;

namespace Geta.Optimizely.Tags.Web;

public class Startup
{
    private readonly Foundation.Startup _foundationStartup;
    private readonly IConfiguration _configuration;

    public Startup(IWebHostEnvironment webHostingEnvironment, IConfiguration configuration)
    {
        _foundationStartup = new Foundation.Startup(webHostingEnvironment, configuration);
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        _foundationStartup.ConfigureServices(services);

        // When no ContentGraph AppKey is configured (e.g. when running via Aspire),
        // replace the Graph sync client with a no-op so startup doesn't fail.
        var graphAppKey = _configuration["Optimizely:ContentGraph:AppKey"];
        if (string.IsNullOrEmpty(graphAppKey))
        {
            var syncClientType = typeof(GraphCmsOptions).Assembly
                .GetType("Optimizely.Graph.Cms.Client.ISyncClient");
            if (syncClientType != null)
            {
                var descriptor = services.FirstOrDefault(d => d.ServiceType == syncClientType);
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // .NET 10 added overloads to DispatchProxy.Create, so GetMethod(name) is now
                // ambiguous. Select the parameterless generic Create<T, TProxy>() explicitly.
                var createMethod = typeof(DispatchProxy).GetMethods()
                    .Single(m => m.Name == nameof(DispatchProxy.Create)
                                 && m.IsGenericMethodDefinition
                                 && m.GetGenericArguments().Length == 2
                                 && m.GetParameters().Length == 0);
                var proxy = createMethod
                    .MakeGenericMethod(syncClientType, typeof(NoOpSyncClientProxy))
                    .Invoke(null, null)!;

                services.AddSingleton(syncClientType, proxy);
            }
        }

        services.AddGetaTags();

        // Serve the addon's client resources (module folder) directly from source
        // so the protected-module zip isn't required during development.
        var moduleName = typeof(ContainerController).Assembly.GetName().Name;
        var fullPath = Path.GetFullPath($"../{moduleName}/module");

        services.Configure<CompositeFileProviderOptions>(options =>
        {
            options.BasePathFileProviders.Add(new MappingPhysicalFileProvider(
                                                  $"/Optimizely/{moduleName}",
                                                  string.Empty,
                                                  fullPath));
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseGetaTags();
        _foundationStartup.Configure(app, env);
    }
}
