
using Newtonsoft.Json;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpcPublisher
{
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using static OpcMonitoredItem;
    using static OpcPublisher.Workarounds.TraceWorkaround;
    using static OpcStackConfiguration;

    public class PublisherTelemetryConfiguration
    {
        public class Settings
        {
            [DefaultValue(true)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
            public bool Publish;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
            public string Pattern;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
            public string Name;
        }

        public class MonitoredItemTelemetryConfiguration
        {
            public MonitoredItemTelemetryConfiguration()
            {
                Flat = true;
                ApplicationUri = new Settings
                {
                    Publish = true,
                    Pattern = null,
                    Name = "ApplicationUri"
                };
                DisplayName = new Settings
                {
                    Publish = true,
                    Pattern = null,
                    Name = "DisplayName"
                };
            }

            [DefaultValue(false)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
            public bool Flat;

            public Settings ApplicationUri;

            public Settings DisplayName;
        }

        public class ValueTelemetryConfiguration
        {
            public ValueTelemetryConfiguration()
            {
                Flat = false;
                Value = new Settings
                {
                    Publish = true,
                    Pattern = null,
                    Name = "Value"
                };
                SourceTimestamp = new Settings
                {
                    Publish = true,
                    Pattern = null,
                    Name = "SourceTimestamp"
                };
                StatusCode = new Settings
                {
                    Publish = false,
                    Pattern = null,
                    Name = "StatusCode"
                };
                Status = new Settings
                {
                    Publish = false,
                    Pattern = null,
                    Name = "Status"
                };
            }

            [DefaultValue(false)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
            public bool Flat;

            public Settings Value;

            public Settings SourceTimestamp;

            public Settings StatusCode;

            public Settings Status;
    }

    public class ServerTelemetryConfiguration
        {
            public ServerTelemetryConfiguration()
            {
                ForEndpointUrl = null;

                EndpointUrl = new Settings
                {
                    Publish = false,
                    Pattern = null,
                    Name = "EndpointUrl"
                };
                NodeId = new Settings
                {
                    Publish = true,
                    Pattern = null,
                    Name = "NodeId"
                };
                MonitoredItem = new MonitoredItemTelemetryConfiguration();
                Value = new ValueTelemetryConfiguration();
            }

            public string ForEndpointUrl { get; set; }

            public Settings EndpointUrl { get; set; }

            public Settings NodeId { get; set; }

            public MonitoredItemTelemetryConfiguration MonitoredItem { get; set; }

            public ValueTelemetryConfiguration Value { get; set; }

        }

        public static string PublisherTelemetryConfigurationFilename
        {
            get => _publisherTelemetryConfigurationFilename;
            set => _publisherTelemetryConfigurationFilename = value;
        }

        public static ServerTelemetryConfiguration GetServerTelemetryConfiguration(string endpointUrl)
        {
            // return the default if there are no others
            if (_serverTelemetryConfigurations == null || string.IsNullOrEmpty(endpointUrl))
            {
                return _defaultServerTelemetryConfiguration;
            }

            // lookup the configuration for the application URI and return the default if there is none
            var serverTelemetryConfiguration = _serverTelemetryConfigurations.FirstOrDefault(c => c.ForEndpointUrl.Equals(endpointUrl, StringComparison.OrdinalIgnoreCase));
            if (serverTelemetryConfiguration == null)
            {
                return _defaultServerTelemetryConfiguration;
            }
            return serverTelemetryConfiguration;
        }

        /// <summary>
        /// Read and parse the publisher node configuration file.
        /// </summary>
        /// <returns></returns>
        public static async Task<bool> ReadConfigAsync()
        {
            // initialize the default server telemetry configuration
            _defaultServerTelemetryConfiguration = new ServerTelemetryConfiguration();

            // return if there is no configuration file specified
            if (string.IsNullOrEmpty(_publisherTelemetryConfigurationFilename))
            {
                Trace("Using default telemetry configuration.");
                return true;
            }

            // get information on the telemetry configuration and validate the json by deserializing it
            try
            {
                Trace($"Attempting to load telemetry configuration file from: {_publisherTelemetryConfigurationFilename}");
                _serverTelemetryConfigurations = JsonConvert.DeserializeObject<List<ServerTelemetryConfiguration>>(File.ReadAllText(_publisherTelemetryConfigurationFilename));

                // use default telemetry configuration from file, if there was one specified
                ServerTelemetryConfiguration defaultServerTelemetryConfiguration = _serverTelemetryConfigurations.FirstOrDefault(c => string.IsNullOrEmpty(c.ForEndpointUrl));
                if (defaultServerTelemetryConfiguration != null)
                {
                    _defaultServerTelemetryConfiguration = defaultServerTelemetryConfiguration;
                    // remove the default configuration
                    _serverTelemetryConfigurations.Remove(defaultServerTelemetryConfiguration);
                }
            }
            catch (Exception e)
            {
                Trace(e, "Loading of the telemetry configuration file failed. Does the file exist and has correct syntax?");
                Trace("exiting...");
                return false;
            }
            return true;
        }

        private static string _publisherTelemetryConfigurationFilename = null;
        private static List<ServerTelemetryConfiguration> _serverTelemetryConfigurations;
        private static ServerTelemetryConfiguration _defaultServerTelemetryConfiguration;
    }
}
