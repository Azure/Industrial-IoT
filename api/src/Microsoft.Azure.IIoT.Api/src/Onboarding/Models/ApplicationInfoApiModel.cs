// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Onboarding.Models {
    using System.Runtime.Serialization;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Application info model
    /// </summary>
    [DataContract]
    public class ApplicationInfoApiModel {

        /// <summary>
        /// Unique application id
        /// </summary>
        [DataMember(Name = "applicationId")]
        public string ApplicationId { get; set; }

        /// <summary>
        /// Type of application
        /// </summary>
        /// <example>Server</example>
        [DataMember(Name = "applicationType")]
        public ApplicationType ApplicationType { get; set; }

        /// <summary>
        /// Unique application uri
        /// </summary>
        [DataMember(Name = "applicationUri")]
        public string ApplicationUri { get; set; }

        /// <summary>
        /// Product uri
        /// </summary>
        [DataMember(Name = "productUri",
            EmitDefaultValue = false)]
        public string ProductUri { get; set; }

        /// <summary>
        /// Default name of application
        /// </summary>
        [DataMember(Name = "applicationName",
            EmitDefaultValue = false)]
        public string ApplicationName { get; set; }

        /// <summary>
        /// Locale of default name - defaults to "en"
        /// </summary>
        [DataMember(Name = "locale",
            EmitDefaultValue = false)]
        public string Locale { get; set; }

        /// <summary>
        /// Localized Names of application keyed on locale
        /// </summary>
        [DataMember(Name = "localizedNames",
            EmitDefaultValue = false)]
        public Dictionary<string, string> LocalizedNames { get; set; }

        /// <summary>
        /// The capabilities advertised by the server.
        /// </summary>
        /// <example>LDS</example>
        /// <example>DA</example>
        [DataMember(Name = "capabilities",
            EmitDefaultValue = false)]
        public HashSet<string> Capabilities { get; set; }

        /// <summary>
        /// Discovery urls of the server
        /// </summary>
        [DataMember(Name = "discoveryUrls",
            EmitDefaultValue = false)]
        public HashSet<string> DiscoveryUrls { get; set; }

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
        /// Host addresses of server application or null
        /// </summary>
        [DataMember(Name = "hostAddresses",
            EmitDefaultValue = false)]
        public HashSet<string> HostAddresses { get; set; }

        /// <summary>
        /// Site of the application
        /// </summary>
        /// <example>productionlineA</example>
        /// <example>cellB</example>
        [DataMember(Name = "siteId",
            EmitDefaultValue = false)]
        public string SiteId { get; set; }

        /// <summary>
        /// Discoverer that registered the application
        /// </summary>
        [DataMember(Name = "discovererId",
            EmitDefaultValue = false)]
        public string DiscovererId { get; set; }

        /// <summary>
        /// Last time application was seen
        /// </summary>
        [DataMember(Name = "notSeenSince",
            EmitDefaultValue = false)]
        public DateTime? NotSeenSince { get; set; }

        /// <summary>
        /// Created
        /// </summary>
        [DataMember(Name = "created",
            EmitDefaultValue = false)]
        public RegistryOperationApiModel Created { get; set; }

        /// <summary>
        /// Updated
        /// </summary>
        [DataMember(Name = "updated",
            EmitDefaultValue = false)]
        public RegistryOperationApiModel Updated { get; set; }
    }
}
