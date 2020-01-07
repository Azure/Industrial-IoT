﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry {

    /// <summary>
    /// Event target constants
    /// </summary>
    public static class EventTargets {

        /// <summary>
        /// Application event type
        /// </summary>
        public const string ApplicationEventTarget = "ApplicationEvent";

        /// <summary>
        /// Application group
        /// </summary>
        public const string Applications = "applications";

        /// <summary>
        /// Endpoint event type
        /// </summary>
        public const string EndpointEventTarget = "EndpointEvent";

        /// <summary>
        /// Endpoint group
        /// </summary>
        public const string Endpoints = "endpoints";

        /// <summary>
        /// Supervisor event type
        /// </summary>
        public const string SupervisorEventTarget = "SupervisorEvent";

        /// <summary>
        /// Supervisors group
        /// </summary>
        public const string Supervisors = "supervisors";

        /// <summary>
        /// Discoverer event type
        /// </summary>
        public const string DiscovererEventTarget = "DiscovererEvent";

        /// <summary>
        /// Discoverers group
        /// </summary>
        public const string Discoverers = "discovery";

        /// <summary>
        /// Publisher event type
        /// </summary>
        public const string PublisherEventTarget = "PublisherEvent";

        /// <summary>
        /// Publishers group
        /// </summary>
        public const string Publishers = "publishers";

        /// <summary>
        /// Discovery progress event targets
        /// </summary>
        public const string DiscoveryProgressTarget = "DiscoveryProgress";
    }
}
