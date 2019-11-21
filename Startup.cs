using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using System;
using System.Collections.Generic;
using System.Linq;

[assembly: FunctionsStartup(typeof(ToggleFa.Startup))]

namespace ToggleFa
{

    class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder) => ConfigureServices(builder.Services);

        public void ConfigureServices(IServiceCollection services)
        {
            // Capture existing configuration providers
            var providers = new List<IConfigurationProvider>();
            foreach (var descriptor in services.Where(des => des.ServiceType == typeof(IConfiguration)).ToList())
            {
                if (!(descriptor.ImplementationInstance is IConfigurationRoot existingConfiguration))
                {
                    continue;
                }
                providers.AddRange(existingConfiguration.Providers);
                services.Remove(descriptor);
            }

            var executionContext = services.BuildServiceProvider().GetService<IOptions<ExecutionContextOptions>>().Value;

            var config = new ConfigurationBuilder()
                .SetBasePath(executionContext.AppDirectory)
                .AddJsonFile("appSettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddAzureAppConfiguration(o =>
                {
                    o.Connect(Environment.GetEnvironmentVariable("ConnectionStrings_AppConfig-Secondary"))
                     .Connect(Environment.GetEnvironmentVariable("ConnectionStrings_AppConfig-Primary"))
                     // List the ones you care about individually, or use a sentinel config key
                     .ConfigureRefresh(r => r.Register("Fa:Settings:Sentinel", refreshAll: true)
                                             .SetCacheExpiration(TimeSpan.FromSeconds(10)))
                     .UseFeatureFlags(c => c.CacheExpirationTime = TimeSpan.FromSeconds(10));


                    //o.Use("Fa:Settings:*", labelFilter: "var1", preferredDateTime: DateTimeOffset.UtcNow)
                    //o.ConnectWithManagedIdentity()
                    //o.SetOfflineCache()
                    //o.UseAzureKeyVault()

                    services.AddSingleton(o.GetRefresher());
                });

            // Add custom providers
            providers.AddRange(config.Build().Providers);
            services.AddSingleton<IConfiguration>(new ConfigurationRoot(providers));

            services.AddFeatureManagement()
                    .AddFeatureFilter<PercentageFilter>()  // Client Side % Filter
                    .AddFeatureFilter<TimeWindowFilter>(); // Client Side Time Filter
        }

    }
}
