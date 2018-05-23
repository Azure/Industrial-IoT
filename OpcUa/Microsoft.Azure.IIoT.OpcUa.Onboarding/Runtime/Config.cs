// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Onboarding.Runtime {
    using Microsoft.Azure.IIoT.Processor;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Collections.Generic;
    using LogLevel = Diagnostics.LogLevel;

    /// <summary>
    /// Web service configuration - wraps a configuration root
    /// </summary>
    public class Config : IEventProcessorConfig, IIoTHubConfig {

        /// <summary>
        /// A configured logger
        /// </summary>
        public ILogger Logger { get; }

        /// <summary>
        /// Configuration
        /// </summary>
        public IConfigurationRoot Configuration { get; }

        /// <summary>
        /// Service configuration
        /// </summary>
        private const string IoThubConnStringKey = "IoTHubConnectionString";
        private const string IoTHubEndpointKey = "IoTHubEndpoint";
        private const string IoTHubManagerV1ApiUrlKey = "IoTHubManagerUrl";
        /// <summary>IoT hub connection string</summary>
        public string IoTHubConnString => GetString(IoThubConnStringKey,
            GetString("_HUB_CS", null));
        /// <summary>Hub event hub endpoint </summary>
        public string HubEndpoint =>
            GetString(IoTHubEndpointKey);
        /// <summary>Hub name </summary>
        public string IoTHubName  {
            get {
                try {
                    return ConnectionString.Parse(IoTHubConnString).HubName;
                }
                catch {
                    return null;
                }
            }
        }

        /// <summary>
        /// Event processor configuration
        /// </summary>
        private const string EventHubConnStringKey = "EventHubConnectionString";
        private const string BlobStorageConnStringKey = "BlobStorageConnectionString";
        private const string LeaseContainerNameKey = "LeaseContainerName";
        private const string EventHubConsumerGroupKey = "EventHubConsumerGroup";
        private const string EventHubPathKey = "EventHubPath";
        private const string UseWebsocketsKey = "UseWebsockets";
        /// <summary> Event hub connection string </summary>
        public string EventHubConnString => GetString(EventHubConnStringKey,
            GetString("_EH_CS", null))?.Trim();
        /// <summary> Event hub consumer group </summary>
        public string ConsumerGroup =>
            GetString(EventHubConsumerGroupKey, "$default");
        /// <summary> Event hub path </summary>
        public string EventHubPath => GetString(EventHubPathKey, IoTHubName);

        /// <summary> Checkpoint storage </summary>
        public string BlobStorageConnString => GetString(BlobStorageConnStringKey,
            GetString("_STORE_CS", null));
        /// <summary> Checkpoint storage </summary>
        public string LeaseContainerName => GetString(LeaseContainerNameKey,
            Namespace);
        /// <summary> Whether use websockets to connect </summary>
        public bool UseWebsockets =>
            GetBool(UseWebsocketsKey, GetBool("_WS", false));

        private const string ReceiveBatchSizeKey = "ReceiveBatchSize";
        private const string ReceiveTimeoutKey = "ReceiveTimeout";
        /// <summary> Receive batch size </summary>
        public int ReceiveBatchSize =>
            GetInt(ReceiveBatchSizeKey, 999);
        /// <summary> Receive timeout </summary>
        public TimeSpan ReceiveTimeout =>
            GetTimeSpan(ReceiveTimeoutKey, TimeSpan.FromSeconds(5));

        private const string NamespaceKey = "StorageNamespace";
        public string Namespace =>
            GetString(NamespaceKey, "opc-onboarding-processor-checkpoints");

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public Config(IConfigurationRoot configuration) {
            Configuration = configuration;
            Logger = new ConsoleLogger(Uptime.ProcessId,
                GetLogLevel("Logging:LogLevel:Default", LogLevel.Debug));
        }

        /// <summary>
        /// Get time span
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TimeSpan GetTimeSpan(string key, TimeSpan? defaultValue = null) {
            if (!TimeSpan.TryParse(GetString(key), out var result)) {
                if (defaultValue != null) {
                    return (TimeSpan)defaultValue;
                }
                throw new InvalidConfigurationException(
                    $"Unable to load timespan value for '{key}' from configuration.");
            }
            return result;
        }

        /// <summary>
        /// Get log level
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private LogLevel GetLogLevel(string key, LogLevel defaultValue) {
            var level = GetString(key);
            if (!string.IsNullOrEmpty(level)) {
                switch (level.ToLowerInvariant()) {
                    case "Warning":
                        return LogLevel.Warn;
                    case "Trace":
                    case "Debug":
                        return LogLevel.Debug;
                    case "Information":
                        return LogLevel.Info;
                    case "Error":
                    case "Critical":
                        return LogLevel.Error;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Read string
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private string GetString(string key, string defaultValue = "") {
            var value = Configuration.GetValue(key, defaultValue);
            if (string.IsNullOrEmpty(value)) {
                return defaultValue;
            }
            return value.Trim();
        }

        /// <summary>
        /// Read boolean
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private bool GetBool(string key, bool defaultValue = false) {
            var value = GetString(key, defaultValue.ToString()).ToLowerInvariant();
            var knownTrue = new HashSet<string> { "true", "t", "yes", "y", "1", "-1" };
            var knownFalse = new HashSet<string> { "false", "f", "no", "n", "0" };
            if (knownTrue.Contains(value)) {
                return true;
            }
            if (knownFalse.Contains(value)) {
                return false;
            }
            return defaultValue;
        }

        /// <summary>
        /// Read int
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private int GetInt(string key, int defaultValue = 0) {
            try {
                return Convert.ToInt32(GetString(key, defaultValue.ToString()));
            }
            catch (Exception e) {
                throw new InvalidConfigurationException(
                    $"Unable to load configuration value for '{key}'", e);
            }
        }
    }
}
