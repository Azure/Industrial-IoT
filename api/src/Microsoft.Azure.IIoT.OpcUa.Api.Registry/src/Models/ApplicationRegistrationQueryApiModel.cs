// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using System.Runtime.Serialization;

    /// <summary>
    /// Application information
    /// </summary>
    [DataContract]
    public class ApplicationRegistrationQueryApiModel {

        /// <summary>
        /// Type of application
        /// </summary>
        [DataMember(Name = "applicationType",
            EmitDefaultValue = false)]
        public ApplicationType? ApplicationType { get; set; }

        /// <summary>
        /// Application uri
        /// </summary>
        [DataMember(Name = "applicationUri",
            EmitDefaultValue = false)]
        public string ApplicationUri { get; set; }

        /// <summary>
        /// Product uri
        /// </summary>
        [DataMember(Name = "productUri",
            EmitDefaultValue = false)]
        public string ProductUri { get; set; }

        /// <summary>
        /// Name of application
        /// </summary>
        [DataMember(Name = "applicationName",
            EmitDefaultValue = false)]
        public string ApplicationName { get; set; }

        /// <summary>
        /// Locale of application name - default is "en"
        /// </summary>
        [DataMember(Name = "locale",
            EmitDefaultValue = false)]
        public string Locale { get; set; }

        /// <summary>
        /// Application capability to query with
        /// </summary>
        [DataMember(Name = "capability",
            EmitDefaultValue = false)]
        public string Capability { get; set; }

        /// <summary>
        /// Discovery profile uri
        /// </summary>
        [DataMember(Name = "discoveryProfileUri",
            EmitDefaultValue = false)]
        public string DiscoveryProfileUri { get; set; }

        /// <summary>
        /// Gateway server uri
        /// </summary>
        [DataMember(Name = "gatewayServerUri",
            EmitDefaultValue = false)]
        public string GatewayServerUri { get; set; }

        /// <summary>
        /// Supervisor or site the application belongs to.
        /// </summary>
        [DataMember(Name = "siteOrGatewayId",
           EmitDefaultValue = false)]
        public string SiteOrGatewayId { get; set; }

        /// <summary>
        /// Whether to include apps that were soft deleted
        /// </summary>
        [DataMember(Name = "includeNotSeenSince",
            EmitDefaultValue = false)]
        public bool? IncludeNotSeenSince { get; set; }

        /// <summary>
        /// Discoverer id to filter with
        /// </summary>
        [DataMember(Name = "discovererId",
            EmitDefaultValue = false)]
        public string DiscovererId { get; set; }
    }
}

