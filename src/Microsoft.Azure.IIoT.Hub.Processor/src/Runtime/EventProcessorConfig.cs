// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Processor.Runtime {
    using Microsoft.Azure.IIoT.Hub.Processor;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Onboarding service configuration - wraps a configuration root
    /// </summary>
    public class EventProcessorConfig : ConfigBase, IEventProcessorConfig {

        /// <summary>
        /// Event processor configuration
        /// </summary>
        private const string kEventHubConnStringKey = "EventHubConnectionString";
        private const string kBlobStorageConnStringKey = "BlobStorageConnectionString";
        private const string kLeaseContainerNameKey = "LeaseContainerName";
        private const string kEventHubConsumerGroupKey = "EventHubConsumerGroup";
        private const string kEventHubPathKey = "EventHubPath";
        private const string kUseWebsocketsKey = "UseWebsockets";
        /// <summary> Event hub connection string </summary>
        public string EventHubConnString {
            get {
                var ep = GetStringOrDefault("PCS_IOTHUBREACT_HUB_ENDPOINT", null);
                if (string.IsNullOrEmpty(ep)) {
                    var cs = GetStringOrDefault(kEventHubConnStringKey, GetStringOrDefault(
                        _serviceId + "_EH_CS", GetStringOrDefault("_EH_CS", null)))?.Trim();
                    if (string.IsNullOrEmpty(cs)) {
                        return null;
                    }
                    return cs;
                }
                if (!ConnectionString.TryParse(IoTHubConnString, out var iothub)) {
                    return null;
                }
                if (ep.StartsWith("Endpoint=", StringComparison.InvariantCultureIgnoreCase)) {
                    ep = ep.Remove(0, "Endpoint=".Length);
                }
                return ConnectionString.CreateEventHubConnectionString(ep,
                    iothub.SharedAccessKeyName, iothub.SharedAccessKey).ToString();
            }
        }

        /// <summary> Event hub consumer group </summary>
        public string ConsumerGroup =>
            GetStringOrDefault(kEventHubConsumerGroupKey, "$default");
        /// <summary> Event hub path </summary>
        public string EventHubPath => GetStringOrDefault(kEventHubPathKey, IoTHubName);

        /// <summary> Checkpoint storage </summary>
        public string BlobStorageConnString {
            get {
                var account = GetStringOrDefault("PCS_ASA_DATA_AZUREBLOB_ACCOUNT",
                    GetStringOrDefault("PCS_IOTHUBREACT_AZUREBLOB_ACCOUNT", null));
                var key = GetStringOrDefault("PCS_ASA_DATA_AZUREBLOB_KEY",
                    GetStringOrDefault("PCS_IOTHUBREACT_AZUREBLOB_KEY", null));
                var suffix = GetStringOrDefault("PCS_ASA_DATA_AZUREBLOB_ENDPOINT_SUFFIX",
                    GetStringOrDefault("PCS_IOTHUBREACT_AZUREBLOB_ENDPOINT_SUFFIX", "core.windows.net"));
                if (string.IsNullOrEmpty(account) || string.IsNullOrEmpty(key)) {
                    var cs = GetStringOrDefault(kBlobStorageConnStringKey, GetStringOrDefault(
                        _serviceId + "_STORE_CS", GetStringOrDefault("_STORE_CS", null)))?.Trim();
                    if (string.IsNullOrEmpty(cs)) {
                        return null;
                    }
                    return cs;
                }
                return "DefaultEndpointsProtocol=https;" +
                    $"EndpointSuffix={suffix};AccountName={account};AccountKey={key}";
            }
        }

        /// <summary> Checkpoint storage </summary>
        public string LeaseContainerName => GetStringOrDefault(kLeaseContainerNameKey,
            Namespace);
        /// <summary> Whether use websockets to connect </summary>
        public bool UseWebsockets => GetBoolOrDefault(kUseWebsocketsKey,
            GetBoolOrDefault(_serviceId + "_WS", GetBoolOrDefault("_WS", false)));

        private const string kReceiveBatchSizeKey = "ReceiveBatchSize";
        private const string kReceiveTimeoutKey = "ReceiveTimeout";
        /// <summary> Receive batch size </summary>
        public int ReceiveBatchSize =>
            GetIntOrDefault(kReceiveBatchSizeKey, 999);
        /// <summary> Receive timeout </summary>
        public TimeSpan ReceiveTimeout =>
            GetDurationOrDefault(kReceiveTimeoutKey, TimeSpan.FromSeconds(5));

        private const string kNamespaceKey = "StorageNamespace";
        /// <summary> Namespace name </summary>
        public string Namespace =>
            GetStringOrDefault(kNamespaceKey, "opc-onboarding-processor-checkpoints");

        private const string kIoTHubConnectionStringKey = "IoTHubConnectionString";
        /// <summary>IoT hub connection string</summary>
        public string IoTHubConnString => GetStringOrDefault(kIoTHubConnectionStringKey,
            GetStringOrDefault(_serviceId + "_HUB_CS",
                GetStringOrDefault("PCS_IOTHUB_CONNSTRING", GetStringOrDefault("_HUB_CS", null))));
        /// <summary>Hub name</summary>
        public string IoTHubName {
            get {
                var name = GetStringOrDefault("PCS_IOTHUBREACT_HUB_NAME", null);
                if (!string.IsNullOrEmpty(name)) {
                    return name;
                }
                try {
                    return ConnectionString.Parse(IoTHubConnString).HubName;
                }
                catch {
                    return null;
                }
            }
        }

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="serviceId"></param>
        /// <param name="configuration"></param>
        public EventProcessorConfig(IConfigurationRoot configuration, string serviceId = "") :
            base(configuration) {
            _serviceId = serviceId ?? throw new ArgumentNullException(nameof(serviceId));
        }

        private readonly string _serviceId;
    }
}
