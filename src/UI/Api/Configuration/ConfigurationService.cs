using ClearMeasure.Bootcamp.Core;

namespace ClearMeasure.Bootcamp.UI.Api.Configuration
{
    public class ConfigurationService
    {
        private readonly ConfigurationModel _configuration;

        public ConfigurationService()
        {
            // Simulate fetching configuration from a database or other source
            _configuration = new ConfigurationModel
            {
                AppInsightsConnectionString = Environment.GetEnvironmentVariable("OpenTelemetry.ConnectionString")
            };
        }

        public ConfigurationModel GetConfiguration()
        {
            return _configuration;
        }
    }
}
