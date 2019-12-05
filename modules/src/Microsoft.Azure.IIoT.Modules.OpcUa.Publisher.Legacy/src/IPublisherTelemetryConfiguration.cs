using System.Collections.Generic;

namespace OpcPublisher
{
    /// <summary>
    /// Interface to control the telemetry publish, name and pattern properties.
    /// </summary>
    public interface ITelemetrySettings
    {
        /// <summary>
        /// Flag to control if the value should be published.
        /// </summary>
        bool? Publish { get; set; }

        /// <summary>
        /// The name under which the telemetry value should be published.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// The pattern which should be applied to the telemetry value.
        /// </summary>
        string Pattern { get; set; }

        /// <summary>
        /// Method to apply the regex to the given value if one is defined, otherwise we return the string passed in.
        /// </summary>
        string PatternMatch(string stringToParse);
    }

    public interface IPublisherTelemetryConfiguration
    {
        /// <summary>
        /// Method to get the telemetry configuration for a specific endpoint URL. If the endpoint URL is not found, then the default configuration is returned.
        /// </summary>
        EndpointTelemetryConfigurationModel GetEndpointTelemetryConfiguration(string endpointUrl);

        /// <summary>
        /// Update the default configuration with the settings give in the 'Defaults' object of the configuration file.
        /// </summary>
        bool UpdateDefaultEndpointTelemetryConfiguration();

        /// <summary>
        /// Update the endpoint specific telemetry configuration using settings from the default configuration.
        /// Only those settings are applied, which are not defined by the endpoint specific configuration.
        /// </summary>
        void UpdateEndpointTelemetryConfiguration(EndpointTelemetryConfigurationModel config);
    }
}
