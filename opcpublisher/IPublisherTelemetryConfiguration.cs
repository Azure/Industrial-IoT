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


    /// <summary>
    /// Class to define the MonitoredItem related telemetry configuration.
    /// </summary>
    public interface IMonitoredItemTelemetryConfiguration
    {
        /// <summary>
        /// Controls if the MonitoredItem object should be flattened.
        /// </summary>
        bool? Flat { get; set; }

        /// <summary>
        /// The ApplicationUri value telemetry configuration.
        /// </summary>
        ITelemetrySettings ApplicationUri { get; set; }

        /// <summary>
        /// The DisplayName value telemetry configuration.
        /// </summary>
        ITelemetrySettings DisplayName { get; set; }
    }

    /// <summary>
    /// Interface to define the Value related telemetry configuration.
    /// </summary>
    public interface IValueTelemetryConfiguration
    {
        /// <summary>
        /// Controls if the Value object should be flattened.
        /// </summary>
        bool? Flat { get; set; }

        /// <summary>
        /// The Value value telemetry configuration.
        /// </summary>
        ITelemetrySettings Value { get; set; }

        /// <summary>
        /// The SourceTimestamp value telemetry configuration.
        /// </summary>
        ITelemetrySettings SourceTimestamp { get; set; }

        /// <summary>
        /// The StatusCode value telemetry configuration.
        /// </summary>
        ITelemetrySettings StatusCode { get; set; }

        /// <summary>
        /// The Status value telemetry configuration.
        /// </summary>
        ITelemetrySettings Status { get; set; }

    }

    /// <summary>
    /// Interface to define the model for the telemetry configuration of an endpoint in the configuration file.
    /// </summary>
    public interface IEndpointTelemetryConfigurationModel
    {
        /// <summary>
        /// Specifies the endpoint URL the telemetry should be configured for.
        /// </summary>
        string ForEndpointUrl { get; set; }

        /// <summary>
        /// Specifies the configuration for the value EndpointUrl.
        /// </summary>
        ITelemetrySettings EndpointUrl { get; set; }

        /// <summary>
        /// Specifies the configuration for the value NodeId.
        /// </summary>
        ITelemetrySettings NodeId { get; set; }

        /// <summary>
        /// Specifies the configuration for the value MonitoredItem.
        /// </summary>
        IMonitoredItemTelemetryConfiguration MonitoredItem { get; set; }

        /// <summary>
        /// Specifies the configuration for the value Value.
        /// </summary>
        IValueTelemetryConfiguration Value { get; set; }
    }

    /// <summary>
    /// Interface to define the telemetryconfiguration.json configuration file layout.
    /// </summary>
    public interface ITelemetryConfigurationFileModel
    {
        /// <summary>
        /// Default settings for all endpoints without specific configuration.
        /// </summary>
        IEndpointTelemetryConfigurationModel Defaults { get; set; }

        /// <summary>
        /// Endpoint specific configuration.
        /// </summary>
        List<IEndpointTelemetryConfigurationModel> EndpointSpecific { get; }
    }

    public interface IPublisherTelemetryConfiguration
    {
        /// <summary>
        /// Method to get the telemetry configuration for a specific endpoint URL. If the endpoint URL is not found, then the default configuration is returned.
        /// </summary>
        IEndpointTelemetryConfigurationModel GetEndpointTelemetryConfiguration(string endpointUrl);

        /// <summary>
        /// Update the default configuration with the settings give in the 'Defaults' object of the configuration file.
        /// </summary>
        bool UpdateDefaultEndpointTelemetryConfiguration();

        /// <summary>
        /// Update the endpoint specific telemetry configuration using settings from the default configuration.
        /// Only those settings are applied, which are not defined by the endpoint specific configuration.
        /// </summary>
        void UpdateEndpointTelemetryConfiguration(IEndpointTelemetryConfigurationModel config);
    }
}
