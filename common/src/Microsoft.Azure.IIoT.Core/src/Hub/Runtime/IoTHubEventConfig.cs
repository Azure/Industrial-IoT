// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Client.Runtime {
    using Microsoft.Azure.IIoT.Messaging.EventHub;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// IoT Hub Event processor configuration - wraps a configuration root
    /// </summary>
    public class IoTHubEventConfig : ConfigBase, IEventHubConsumerConfig {

        /// <summary>
        /// Event processor configuration
        /// </summary>
        private const string kIoTHubConnectionStringKey = "IoTHubConnectionString";
        private const string kEventHubConnStringKey = "EventHubConnectionString";
        private const string kEventHubConsumerGroupKey = "EventHubConsumerGroup";

        private const string kEventHubPathKey = "EventHubPath";
        private const string kUseWebsocketsKey = "UseWebsockets";

        /// <summary> Event hub connection string </summary>
        public string EventHubConnString {
            get {
                var ep = GetStringOrDefault(PcsVariable.PCS_IOTHUB_EVENTHUBENDPOINT,
                    () => GetStringOrDefault("PCS_IOTHUBREACT_HUB_ENDPOINT",
                    () => null));
                if (string.IsNullOrEmpty(ep)) {
                    var cs = GetStringOrDefault(kEventHubConnStringKey,
                        () => GetStringOrDefault(_serviceId + "_EH_CS",
                        () => GetStringOrDefault("_EH_CS",
                        () => null)))?.Trim();
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

        /// <summary> Event hub default consumer group </summary>
        public string ConsumerGroup => GetStringOrDefault(kEventHubConsumerGroupKey,
            () => GetStringOrDefault("PCS_IOTHUB_EVENTHUBCONSUMERGROUP",
                () => GetStringOrDefault("PCS_IOTHUBREACT_HUB_CONSUMERGROUP", () => "$default")));
        /// <summary> Event hub path </summary>
        public string EventHubPath => GetStringOrDefault(kEventHubPathKey,
            () => IoTHubName);
        /// <summary> Whether use websockets to connect </summary>
        public bool UseWebsockets => GetBoolOrDefault(kUseWebsocketsKey,
            () => GetBoolOrDefault(_serviceId + "_WS",
                () => GetBoolOrDefault("_WS", () => false)));

        /// <summary>IoT hub connection string</summary>
        public string IoTHubConnString => GetStringOrDefault(kIoTHubConnectionStringKey,
            () => GetStringOrDefault(_serviceId + "_HUB_CS",
               () => GetStringOrDefault(PcsVariable.PCS_IOTHUB_CONNSTRING,
                   () => GetStringOrDefault("_HUB_CS", () => null))));
        /// <summary>Hub name</summary>
        public string IoTHubName {
            get {
                var name = GetStringOrDefault("PCS_IOTHUBREACT_HUB_NAME", () => null);
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
        /// <param name="configuration"></param>
        /// <param name="serviceId"></param>
        public IoTHubEventConfig(IConfiguration configuration, string serviceId = "") :
            base(configuration) {
            _serviceId = serviceId ?? throw new ArgumentNullException(nameof(serviceId));
        }

        private readonly string _serviceId;
    }
}
