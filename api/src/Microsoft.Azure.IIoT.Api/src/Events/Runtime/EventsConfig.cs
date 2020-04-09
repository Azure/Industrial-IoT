// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Events.Runtime {
    using Microsoft.Azure.IIoT.OpcUa.Api.Events;
    using Microsoft.Azure.IIoT.Api.Runtime;
    using Microsoft.Azure.IIoT.Messaging.SignalR;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Configuration - wraps a configuration root
    /// </summary>
    public class EventsConfig : ApiConfigBase, IEventsConfig, ISignalRClientConfig {

        /// <summary>
        /// Events configuration
        /// </summary>
        private const string kEventsServiceUrlKey = "EventsServiceUrl";
        private const string kEventsServiceIdKey = "EventsServiceResourceId";

        /// <summary>Events configuration endpoint</summary>
        public string OpcUaEventsServiceUrl => GetStringOrDefault(
            kEventsServiceUrlKey,
            () => GetStringOrDefault(PcsVariable.PCS_EVENTS_SERVICE_URL,
                () => GetDefaultUrl("9050", "events")));
        /// <summary>Events service audience</summary>
        public string OpcUaEventsServiceResourceId => GetStringOrDefault(
            kEventsServiceIdKey,
            () => GetStringOrDefault("EVENTS_APP_ID",
                () => GetStringOrDefault(PcsVariable.PCS_AUTH_AUDIENCE,
                    () => null)));

        /// <summary> Use message pack </summary>
        public bool UseMessagePackProtocol => false;

        /// <inheritdoc/>
        public EventsConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
