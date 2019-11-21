using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.FeatureManagement;

namespace ToggleFa
{
    public class Function1
    {
        // https://github.com/microsoft/FeatureManagement-Dotnet

        private readonly IConfiguration _config;
        private readonly IConfigurationRefresher _configRefresher;
        private readonly IFeatureManager _features;
        // An IFeatureManagerSnapshot is a request-static view of features

        public Function1(IConfiguration config, IConfigurationRefresher configRefresher, IFeatureManager features)
        {
            _config = config;
            _configRefresher = configRefresher;
            _features = features;
        }

        [FunctionName("value")]
        public async Task<string> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req)
        {
            // Signal to refresh the configuration watched keys are modified. This will be no-op
            // if the cache expiration time window is not reached.
            _configRefresher.Refresh(); // Can do sync by adding await

            // CONFIG (standard)
            var configValue = _config[ConfigKeys.FnMessage];

            // CONFIG (from KV)
            var kvConfigValue = _config[ConfigKeys.AKVSourcedSecret];

            // FEATURE
            var featureValue = _features.IsEnabled(Features.FlipFlop);

            return $"Config: {configValue} || Feature: {featureValue} || From KV: {kvConfigValue}";
        }
    }
}
