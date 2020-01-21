// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;

    /// <summary>
    /// Application information
    /// </summary>
    public class ApplicationRegistrationQueryModel {

        /// <summary>
        /// Type of application
        /// </summary>
        public ApplicationType? ApplicationType { get; set; }

        /// <summary>
        /// Application uri
        /// </summary>
        public string ApplicationUri { get; set; }

        /// <summary>
        /// Product uri
        /// </summary>
        public string ProductUri { get; set; }

        /// <summary>
        /// Name of application
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// Locale of application name
        /// </summary>
        public string Locale { get; set; }

        /// <summary>
        /// Application capability to query
        /// </summary>
        public string Capability { get; set; }

        /// <summary>
        /// Discovery profile uri
        /// </summary>
        public string DiscoveryProfileUri { get; set; }

        /// <summary>
        /// Gateway server uri
        /// </summary>
        public string GatewayServerUri { get; set; }

        /// <summary>
        /// Supervisor or site the application belongs to.
        /// </summary>
        public string SiteOrGatewayId { get; set; }

        /// <summary>
        /// Whether to include applications that were soft deleted
        /// </summary>
        public bool? IncludeNotSeenSince { get; set; }

        /// <summary>
        /// Discoverer id to filter with
        /// </summary>
        public string DiscovererId { get; set; }
    }
}

