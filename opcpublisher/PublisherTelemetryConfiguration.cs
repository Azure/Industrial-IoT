
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpcPublisher
{
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using static Program;

    public class PublisherTelemetryConfiguration
    {
        public const string EndpointUrlNameDefault = "EndpointUrl";
        public const string NodeIdNameDefault = "NodeId";
        public const string ApplicationUriNameDefault = "ApplicationUri";
        public const string DisplayNameNameDefault = "DisplayName";
        public const string ValueNameDefault = "Value";
        public const string SourceTimestampNameDefault = "SourceTimestamp";
        public const string StatusNameDefault = "Status";
        public const string StatusCodeNameDefault = "StatusCode";

        /// <summary>
        /// Class to control the telemetry publish, name and pattern properties.
        /// </summary>
        public class Settings
        {
            public bool? Publish
            {
                get => _publish;
                set
                {
                    if (value != null)
                    {
                        _publish = value;
                    }
                }
            }

            public string Name
            {
                get => _name;
                set
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        _name = value;
                    }
                }
            }

            public string Pattern
            {
                get => _pattern;
                set
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        // validate pattern
                        try
                        {
                            _patternRegex = new Regex(value);
                            _pattern = value;
                        }
                        catch
                        {
                            Logger.Fatal($"The regular expression '{value}' used for the property 'Pattern' is not a valid regular expression. Please change.");
                            throw new Exception($"The regular expression '{value}' used for the property 'Pattern' is not a valid regular expression. Please change.");
                        }
                    }
                }
            }

            public Settings()
            {
                _publish = null;
                _name = null;
                _pattern = null;
                _patternRegex = null;
            }

            /// <summary>
            /// Method to apply the regex to the given value if one is defined, otherwise we return the string passed in.
            /// </summary>
            /// <param name="stringToParse"></param>
            /// <returns></returns>
            public string PatternMatch(string stringToParse)
            {
                // no pattern set, return full string
                if (_patternRegex == null)
                {
                    return stringToParse;
                }

                // build the result string based on the pattern
                string result = string.Empty;
                Match match = _patternRegex.Match(stringToParse);
                if (match.Groups[0].Success)
                {
                    foreach (var group in match.Groups.Skip(1))
                    {
                        result += group.Value;
                    }
                }
                return result;
            }

            private bool? _publish;
            private string _name;
            private string _pattern;
            private Regex _patternRegex;

        }

        /// <summary>
        /// Class to define the MonitoredItem related telemetry configuration.
        /// </summary>
        public class MonitoredItemTelemetryConfiguration
        {
            public MonitoredItemTelemetryConfiguration()
            {
                _flat = null;
                _applicationUri = new Settings();
                _displayName = new Settings();
            }

            public bool? Flat
            {
                get => _flat;
                set
                {
                    if (value != null)
                    {
                        _flat = value;
                    }
                }
            }

            public Settings ApplicationUri
            {
                get => _applicationUri;
                set
                {
                    _applicationUri.Publish = value.Publish;
                    _applicationUri.Name = value.Name;
                    _applicationUri.Pattern = value.Pattern;
                }
            }


            public Settings DisplayName
            {
                get => _displayName;
                set
                {
                    _displayName.Publish = value.Publish;
                    _displayName.Name = value.Name;
                    _displayName.Pattern = value.Pattern;
                }
            }

            private bool? _flat;
            private Settings _applicationUri;
            private Settings _displayName;
        }

        /// <summary>
        /// Class to define the Value related telemetry configuration.
        /// </summary>
        public class ValueTelemetryConfiguration
        {
            public ValueTelemetryConfiguration()
            {
                _flat = null;
                _value = new Settings();
                _sourceTimestamp = new Settings();
                _statusCode = new Settings();
                _status = new Settings();
            }

            public bool? Flat
            {
                get => _flat;
                set
                {
                    if (value != null)
                    {
                        _flat = value;
                    }
                }
            }

            public Settings Value
            {
                get => _value;
                set
                {
                    _value.Publish = value.Publish;
                    _value.Name = value.Name;
                    _value.Pattern = value.Pattern;
                }
            }

            public Settings SourceTimestamp
            {
                get => _sourceTimestamp;
                set
                {
                    _sourceTimestamp.Publish = value.Publish;
                    _sourceTimestamp.Name = value.Name;
                    _sourceTimestamp.Pattern = value.Pattern;
                }
            }

            public Settings StatusCode
            {
                get => _statusCode;
                set
                {
                    _statusCode.Publish = value.Publish;
                    _statusCode.Name = value.Name;
                    _statusCode.Pattern = value.Pattern;
                }
            }

            public Settings Status
            {
                get => _status;
                set
                {
                    _status.Publish = value.Publish;
                    _status.Name = value.Name;
                    _status.Pattern = value.Pattern;
                }
            }

            private bool? _flat;
            private Settings _value;
            private Settings _sourceTimestamp;
            private Settings _statusCode;
            private Settings _status;
        }

        /// <summary>
        /// Class to define the publisher configuration related telemetry configuration.
        /// </summary>
        public class EndpointTelemetryConfiguration
        {
            public EndpointTelemetryConfiguration()
            {
                ForEndpointUrl = null;
                _endpointUrl = new Settings();
                _nodeId = new Settings();
                _monitoredItem = new MonitoredItemTelemetryConfiguration();
                _value = new ValueTelemetryConfiguration();
            }

            public string ForEndpointUrl { get; set; }

            public Settings EndpointUrl
            {
                get => _endpointUrl;
                set
                {
                    _endpointUrl.Publish = value.Publish;
                    _endpointUrl.Name = value.Name;
                    _endpointUrl.Pattern = value.Pattern;
                }
            }

            public Settings NodeId
            {
                get => _nodeId;
                set
                {
                    _nodeId.Publish = value.Publish;
                    _nodeId.Name = value.Name;
                    _nodeId.Pattern = value.Pattern;
                }
            }

            public MonitoredItemTelemetryConfiguration MonitoredItem
            {
                get => _monitoredItem;
                set
                {
                    _monitoredItem.Flat = value.Flat;
                    _monitoredItem.ApplicationUri = value.ApplicationUri;
                    _monitoredItem.DisplayName = value.DisplayName;
                }
            }

            public ValueTelemetryConfiguration Value
            {
                get => _value;
                set
                {
                    _value.Flat = value.Flat;
                    _value.Value = value.Value;
                    _value.SourceTimestamp = value.SourceTimestamp;
                    _value.StatusCode = value.StatusCode;
                    _value.Status = value.Status;
                }
            }

            private Settings _endpointUrl;
            private MonitoredItemTelemetryConfiguration _monitoredItem;
            private ValueTelemetryConfiguration _value;
            private Settings _nodeId;
        }

        /// <summary>
        /// Class to define the telemetryconfiguration.json configuration file layout.
        /// </summary>
        public class TelemetryConfiguration
        {
            public EndpointTelemetryConfiguration Defaults;
            public List<EndpointTelemetryConfiguration> EndpointSpecific;

            public TelemetryConfiguration()
            {
                EndpointSpecific = new List<EndpointTelemetryConfiguration>();
            }
        }

        public static string PublisherTelemetryConfigurationFilename { get; set; } = null;

        /// <summary>
        /// Initialize resources for the telemetry configuration.
        /// </summary>
        public static void Init(CancellationToken shutdownToken)
        {
            _telemetryConfiguration = null;
            _endpointTelemetryConfigurations = new List<EndpointTelemetryConfiguration>();
            _defaultEndpointTelemetryConfiguration = null;
            _endpointTelemetryConfigurationCache = new Dictionary<string, EndpointTelemetryConfiguration>();
            _shutdownToken = shutdownToken;
        }

        /// <summary>
        /// Frees resources for the telemetry configuration.
        /// </summary>
        public static void Deinit()
        {
            PublisherTelemetryConfigurationFilename = null;
            _telemetryConfiguration = null;
            _endpointTelemetryConfigurations = null;
            _defaultEndpointTelemetryConfiguration = null;
            _endpointTelemetryConfigurationCache = null;
        }


        /// <summary>
        /// Method to get the telemetry configuration for a specific endpoint URL. If the endpoint URL is not found, then the default configuration is returned.
        /// </summary>
        public static EndpointTelemetryConfiguration GetEndpointTelemetryConfiguration(string endpointUrl)
        {
            // lookup configuration in cache and return it or return default configuration
            if (_endpointTelemetryConfigurationCache.ContainsKey(endpointUrl))
            {
                return _endpointTelemetryConfigurationCache[endpointUrl];
            }
            return _defaultEndpointTelemetryConfiguration;
        }

        /// <summary>
        /// Validate the endpoint configuration. 'Name' and 'Flat' properties are not allowed and there is only one configuration per endpoint allowed.
        /// </summary>
        private static bool ValidateEndpointConfiguration(EndpointTelemetryConfiguration config)
        {
            if (config.ForEndpointUrl == null)
            {
                Logger.Fatal("Each object in the 'EndpointSpecific' array must have a property 'ForEndpointUrl'. Please change.");
                return false;

            }
            if (_telemetryConfiguration.EndpointSpecific.Count(c => !string.IsNullOrEmpty(c.ForEndpointUrl) && c.ForEndpointUrl.Equals(config?.ForEndpointUrl, StringComparison.OrdinalIgnoreCase)) > 1)
            {
                Logger.Fatal($"The value '{config.ForEndpointUrl}' for property 'ForEndpointUrl' is only allowed to used once in the 'EndpointSpecific' array. Please change.");
                return false;
            }
            if (config.EndpointUrl.Name != null || config.NodeId.Name != null ||
                config.MonitoredItem.ApplicationUri.Name != null || config.MonitoredItem.DisplayName.Name != null ||
                config.Value.Value.Name != null || config.Value.SourceTimestamp.Name != null || config.Value.StatusCode.Name != null || config.Value.Status.Name != null)
            {
                Logger.Fatal("The property 'Name' is not allowed in any object in the 'EndpointSpecific' array. Please change.");
                return false;
            }
            if (config.MonitoredItem.Flat != null || config.Value.Flat != null)
            {
                Logger.Fatal("The property 'Flat' is not allowed in any object in the 'EndpointSpecific' array. Please change.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Initialize the default configuration to be compatible with Connected factory Preconfigured Solution.
        /// </summary>
        private static void InitializePublisherDefaultEndpointTelemetryConfiguration()
        {
            // create the default configuration
            _defaultEndpointTelemetryConfiguration = new EndpointTelemetryConfiguration();

            // set defaults for 'Name' to be compatible with Connected factory
            _defaultEndpointTelemetryConfiguration.EndpointUrl.Name = EndpointUrlNameDefault;
            _defaultEndpointTelemetryConfiguration.NodeId.Name = NodeIdNameDefault;
            _defaultEndpointTelemetryConfiguration.MonitoredItem.ApplicationUri.Name = ApplicationUriNameDefault;
            _defaultEndpointTelemetryConfiguration.MonitoredItem.DisplayName.Name = DisplayNameNameDefault;
            _defaultEndpointTelemetryConfiguration.Value.Value.Name = ValueNameDefault;
            _defaultEndpointTelemetryConfiguration.Value.SourceTimestamp.Name = SourceTimestampNameDefault;
            _defaultEndpointTelemetryConfiguration.Value.StatusCode.Name = StatusCodeNameDefault;
            _defaultEndpointTelemetryConfiguration.Value.Status.Name = StatusCodeNameDefault;

            // set defaults for 'Publish' to be compatible with Connected factory
            _defaultEndpointTelemetryConfiguration.EndpointUrl.Publish = false;
            _defaultEndpointTelemetryConfiguration.NodeId.Publish = true;
            _defaultEndpointTelemetryConfiguration.MonitoredItem.ApplicationUri.Publish = true;
            _defaultEndpointTelemetryConfiguration.MonitoredItem.DisplayName.Publish = true;
            _defaultEndpointTelemetryConfiguration.Value.Value.Publish = true;
            _defaultEndpointTelemetryConfiguration.Value.SourceTimestamp.Publish = true;
            _defaultEndpointTelemetryConfiguration.Value.StatusCode.Publish = false;
            _defaultEndpointTelemetryConfiguration.Value.Status.Publish = false;

            // set defaults for 'Flat' to be compatible with Connected factory
            _defaultEndpointTelemetryConfiguration.MonitoredItem.Flat = true;
            _defaultEndpointTelemetryConfiguration.Value.Flat = true;

            // 'Pattern' is set to null on creation which is whats default
        }

        /// <summary>
        /// Update the default configuration with the settings give in the 'Defaults' object of the configuration file.
        /// </summary>
        public static bool UpdateDefaultEndpointTelemetryConfiguration()
        {
            // sanity check user default configuration
            if (_telemetryConfiguration.Defaults != null)
            {
                if (_telemetryConfiguration.Defaults.ForEndpointUrl != null)
                {
                    Logger.Fatal("The property 'ForEndpointUrl' is not allowed in 'Defaults'. Please change.");
                    return false;
                }

                // process all properties
                _defaultEndpointTelemetryConfiguration.EndpointUrl = _telemetryConfiguration.Defaults.EndpointUrl;
                _defaultEndpointTelemetryConfiguration.NodeId = _telemetryConfiguration.Defaults.NodeId;
                _defaultEndpointTelemetryConfiguration.MonitoredItem = _telemetryConfiguration.Defaults.MonitoredItem;
                _defaultEndpointTelemetryConfiguration.Value = _telemetryConfiguration.Defaults.Value;
            }
            return true;
        }

        /// <summary>
        /// Update the endpoint specific telemetry configuration using settings from the default configuration.
        /// Only those settings are applied, which are not defined by the endpoint specific configuration.
        /// </summary>
        public static void UpdateEndpointTelemetryConfiguration(EndpointTelemetryConfiguration config)
        {
            // process all properties, applying only those defaults which are not set in the endpoint specific config
            config.EndpointUrl.Name = config.EndpointUrl.Name ?? _defaultEndpointTelemetryConfiguration.EndpointUrl.Name;
            config.EndpointUrl.Publish = config.EndpointUrl.Publish ?? _defaultEndpointTelemetryConfiguration.EndpointUrl.Publish;
            config.EndpointUrl.Pattern = config.EndpointUrl.Pattern ?? _defaultEndpointTelemetryConfiguration.EndpointUrl.Pattern;

            config.NodeId.Name = config.NodeId.Name ?? _defaultEndpointTelemetryConfiguration.NodeId.Name;
            config.NodeId.Publish = config.NodeId.Publish ?? _defaultEndpointTelemetryConfiguration.NodeId.Publish;
            config.NodeId.Pattern = config.NodeId.Pattern ?? _defaultEndpointTelemetryConfiguration.NodeId.Pattern;

            config.MonitoredItem.Flat = config.MonitoredItem.Flat ?? _defaultEndpointTelemetryConfiguration.MonitoredItem.Flat;

            config.MonitoredItem.ApplicationUri.Name = config.MonitoredItem.ApplicationUri.Name ?? _defaultEndpointTelemetryConfiguration.MonitoredItem.ApplicationUri.Name;
            config.MonitoredItem.ApplicationUri.Publish = config.MonitoredItem.ApplicationUri.Publish ?? _defaultEndpointTelemetryConfiguration.MonitoredItem.ApplicationUri.Publish;
            config.MonitoredItem.ApplicationUri.Pattern = config.MonitoredItem.ApplicationUri.Pattern ?? _defaultEndpointTelemetryConfiguration.MonitoredItem.ApplicationUri.Pattern;

            config.MonitoredItem.DisplayName.Name = config.MonitoredItem.DisplayName.Name ?? _defaultEndpointTelemetryConfiguration.MonitoredItem.DisplayName.Name;
            config.MonitoredItem.DisplayName.Publish = config.MonitoredItem.DisplayName.Publish ?? _defaultEndpointTelemetryConfiguration.MonitoredItem.DisplayName.Publish;
            config.MonitoredItem.DisplayName.Pattern = config.MonitoredItem.DisplayName.Pattern ?? _defaultEndpointTelemetryConfiguration.MonitoredItem.DisplayName.Pattern;

            config.Value.Flat = config.Value.Flat ?? _defaultEndpointTelemetryConfiguration.Value.Flat;

            config.Value.Value.Name = config.Value.Value.Name ?? _defaultEndpointTelemetryConfiguration.Value.Value.Name;
            config.Value.Value.Publish = config.Value.Value.Publish ?? _defaultEndpointTelemetryConfiguration.Value.Value.Publish;
            config.Value.Value.Pattern = config.Value.Value.Pattern ?? _defaultEndpointTelemetryConfiguration.Value.Value.Pattern;

            config.Value.SourceTimestamp.Name = config.Value.SourceTimestamp.Name ?? _defaultEndpointTelemetryConfiguration.Value.SourceTimestamp.Name;
            config.Value.SourceTimestamp.Publish = config.Value.SourceTimestamp.Publish ?? _defaultEndpointTelemetryConfiguration.Value.SourceTimestamp.Publish;
            config.Value.SourceTimestamp.Pattern = config.Value.SourceTimestamp.Pattern ?? _defaultEndpointTelemetryConfiguration.Value.SourceTimestamp.Pattern;

            config.Value.StatusCode.Name = config.Value.StatusCode.Name ?? _defaultEndpointTelemetryConfiguration.Value.StatusCode.Name;
            config.Value.StatusCode.Publish = config.Value.StatusCode.Publish ?? _defaultEndpointTelemetryConfiguration.Value.StatusCode.Publish;
            config.Value.StatusCode.Pattern = config.Value.StatusCode.Pattern ?? _defaultEndpointTelemetryConfiguration.Value.StatusCode.Pattern;

            config.Value.Status.Name = config.Value.Status.Name ?? _defaultEndpointTelemetryConfiguration.Value.Status.Name;
            config.Value.Status.Publish = config.Value.Status.Publish ?? _defaultEndpointTelemetryConfiguration.Value.Status.Publish;
            config.Value.Status.Pattern = config.Value.Status.Pattern ?? _defaultEndpointTelemetryConfiguration.Value.Status.Pattern;
        }

        /// <summary>
        /// Read and parse the publisher telemetry configuration file.
        /// </summary>
        public static async Task<bool> ReadConfigAsync()
        {
            // initialize with the default server telemetry configuration
           InitializePublisherDefaultEndpointTelemetryConfiguration();

            // return if there is no configuration file specified
            if (string.IsNullOrEmpty(PublisherTelemetryConfigurationFilename))
            {
                Logger.Information("Using default telemetry configuration.");
                return true;
            }

            // get information on the telemetry configuration
            try
            {
                Logger.Information($"Attempting to load telemetry configuration file from: {PublisherTelemetryConfigurationFilename}");
                _telemetryConfiguration = JsonConvert.DeserializeObject<TelemetryConfiguration>(await File.ReadAllTextAsync(PublisherTelemetryConfigurationFilename));

                // update the default configuration with the 'Defaults' settings from the configuration file
                if (UpdateDefaultEndpointTelemetryConfiguration() == false)
                {
                    return false;
                }

                // sanity check all endpoint specific configurations and add them to the lookup dictionary
                foreach (var config in _telemetryConfiguration.EndpointSpecific)
                {
                    // validate the endpoint specific telemetry configuration
                    if (ValidateEndpointConfiguration(config) == false)
                    {
                        return false;
                    }

                    // set defaults for unset values
                    UpdateEndpointTelemetryConfiguration(config);

                    // add the endpoint configuration to the lookup cache
                    _endpointTelemetryConfigurationCache.Add(config.ForEndpointUrl, config);
                }
            }
            catch (Exception e)
            {
                Logger.Fatal(e, "Loading of the telemetry configuration file failed. Does the file exist and has correct syntax? Exiting...");
                return false;
            }
            return true;
        }

        private static TelemetryConfiguration _telemetryConfiguration;
        private static List<EndpointTelemetryConfiguration> _endpointTelemetryConfigurations;
        private static EndpointTelemetryConfiguration _defaultEndpointTelemetryConfiguration;
        private static Dictionary<string, EndpointTelemetryConfiguration> _endpointTelemetryConfigurationCache;
        private static CancellationToken _shutdownToken;
    }
}
