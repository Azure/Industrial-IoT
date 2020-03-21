// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging.EventHub.Runtime {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Event hub configuration - wraps a configuration root
    /// </summary>
    public class EventHubConsumerConfig : ConfigBase, IEventHubConsumerConfig {

        /// <summary>
        /// Event hub configuration
        /// </summary>
        private const string kEventHubConnStringKey = "EventHubConnectionString";
        private const string kEventHubNameKey = "EventHubName";
        private const string kEventHubConsumerGroupKey = "EventHubConsumerGroup";
        private const string kUseWebsocketsKey = "UseWebsockets";

        /// <summary> Event hub connection string </summary>
        public string EventHubConnString => GetStringOrDefault(kEventHubConnStringKey,
            () => GetStringOrDefault(PcsVariable.PCS_EVENTHUB_CONNSTRING,
                () => null));
        /// <summary> Event hub path </summary>
        public string EventHubPath => GetStringOrDefault(kEventHubNameKey,
            () => GetStringOrDefault(PcsVariable.PCS_EVENTHUB_NAME,
                () => null));
        /// <summary> Whether use websockets to connect </summary>
        public bool UseWebsockets => GetBoolOrDefault(kUseWebsocketsKey,
            () => GetBoolOrDefault("PCS_EVENTHUB_USE_WEBSOCKET",
                () => GetBoolOrDefault("_WS",
                () => false)));
        /// <summary> Event hub consumer group </summary>
        public string ConsumerGroup => GetStringOrDefault(kEventHubConsumerGroupKey,
            () => GetStringOrDefault("PCS_EVENTHUB_CONSUMERGROUP",
                () => "$default"));

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public EventHubConsumerConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
