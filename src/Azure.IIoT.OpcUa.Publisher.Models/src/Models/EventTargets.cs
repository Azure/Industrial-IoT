// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    /// <summary>
    /// Event target constants
    /// </summary>
    public static class EventTargets
    {
        /// <summary>
        /// Discovery progress event targets
        /// </summary>
        public const string PublisherSampleTarget = "PublisherMessage";

        /// <summary>
        /// Application event type
        /// </summary>
        public const string ApplicationEventTarget = "ApplicationEvent";

        /// <summary>
        /// Endpoint event type
        /// </summary>
        public const string EndpointEventTarget = "EndpointEvent";

        /// <summary>
        /// Supervisor event type
        /// </summary>
        public const string SupervisorEventTarget = "SupervisorEvent";

        /// <summary>
        /// Gateway event type
        /// </summary>
        public const string GatewayEventTarget = "GatewayEvent";

        /// <summary>
        /// Discoverer event type
        /// </summary>
        public const string DiscovererEventTarget = "DiscovererEvent";

        /// <summary>
        /// Publisher event type
        /// </summary>
        public const string PublisherEventTarget = "PublisherEvent";

        /// <summary>
        /// Discovery progress event targets
        /// </summary>
        public const string DiscoveryProgressTarget = "DiscoveryProgress";
    }
}
