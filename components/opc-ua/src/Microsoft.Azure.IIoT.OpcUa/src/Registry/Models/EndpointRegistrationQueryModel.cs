// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;

    /// <summary>
    /// Endpoint query
    /// </summary>
    public class EndpointRegistrationQueryModel {

        /// <summary>
        /// Endoint url for direct server access
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Certificate thumbprint of the endpoint
        /// </summary>
        public string Certificate { get; set; }

        /// <summary>
        /// Endpoint security policy to use - null = Best.
        /// </summary>
        public string SecurityPolicy { get; set; }

        /// <summary>
        /// Security mode to use for communication - null = Best
        /// </summary>
        public SecurityMode? SecurityMode { get; set; }

        /// <summary>
        /// Whether the endpoint is activated
        /// </summary>
        public bool? Activated { get; set; }

        /// <summary>
        /// Whether the endpoint is connected
        /// </summary>
        public bool? Connected { get; set; }

        /// <summary>
        /// The last state of the the activated endpoint
        /// </summary>
        public EndpointConnectivityState? EndpointState { get; set; }

        /// <summary>
        /// Whether to include endpoints that were soft deleted
        /// </summary>
        public bool? IncludeNotSeenSince { get; set; }

        /// <summary>
        /// Discoverer id to filter with
        /// </summary>
        public string DiscovererId { get; set; }

        /// <summary>
        /// Application id to filter
        /// </summary>
        public string ApplicationId { get; set; }

        /// <summary>
        /// Supervisor id to filter with
        /// </summary>
        public string SupervisorId { get; set; }

        /// <summary>
        /// Site or gateway id to filter with
        /// </summary>
        public string SiteOrGatewayId { get; set; }
    }
}

