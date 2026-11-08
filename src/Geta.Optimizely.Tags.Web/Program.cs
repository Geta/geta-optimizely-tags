using Geta.Optimizely.Tags.Web;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.Memory;

Host.CreateDefaultBuilder(args)
    .ConfigureCmsDefaults()
    .ConfigureAppConfiguration((_, config) => UseSandboxGraphSettings(config))
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.UseStartup<Startup>();
        webBuilder.UseContentRoot(Path.GetFullPath("../../sub/geta-foundation-core/src/Foundation"));
    })
    .Build()
    .Run();

// The content root is the Foundation project, so its appsettings.json (including its own Optimizely Graph keys) is
// loaded instead of this project's. Layer this project's Optimizely:ContentGraph section on top of the appsettings
// files, so the sandbox runs without Graph credentials by default. Environment variables and the command line still
// take precedence.
static void UseSandboxGraphSettings(IConfigurationBuilder config)
{
    var graphSettings = new ConfigurationBuilder()
        .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: true)
        .Build()
        .GetSection("Optimizely:ContentGraph")
        .AsEnumerable()
        .Where(setting => setting.Value != null);

    var afterAppSettings = config.Sources.ToList().FindLastIndex(source => source is JsonConfigurationSource) + 1;
    config.Sources.Insert(afterAppSettings, new MemoryConfigurationSource { InitialData = graphSettings });
}
