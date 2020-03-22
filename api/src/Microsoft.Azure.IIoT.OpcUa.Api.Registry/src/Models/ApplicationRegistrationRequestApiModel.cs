// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using System.Runtime.Serialization;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Application information
    /// </summary>
    [DataContract]
    public class ApplicationRegistrationRequestApiModel {

        /// <summary>
        /// Unique application uri
        /// </summary>
        [DataMember(Name = "applicationUri")]
        [Required]
        public string ApplicationUri { get; set; }

        /// <summary>
        /// Type of application
        /// </summary>
        /// <example>Server</example>
        [DataMember(Name = "applicationType",
            EmitDefaultValue = false)]
        public ApplicationType? ApplicationType { get; set; }

        /// <summary>
        /// Product uri of the application.
        /// </summary>
        /// <example>http://contoso.com/fridge/1.0</example>
        [DataMember(Name = "productUri",
            EmitDefaultValue = false)]
        public string ProductUri { get; set; }

        /// <summary>
        /// Default name of the server or client.
        /// </summary>
        [DataMember(Name = "applicationName",
            EmitDefaultValue = false)]
        public string ApplicationName { get; set; }

        /// <summary>
        /// Locale of default name
        /// </summary>
        [DataMember(Name = "locale",
            EmitDefaultValue = false)]
        public string Locale { get; set; }

        /// <summary>
        /// Site of the application
        /// </summary>
        [DataMember(Name = "siteId",
            EmitDefaultValue = false)]
        public string SiteId { get; set; }

        /// <summary>
        /// Localized names key off locale id.
        /// </summary>
        [DataMember(Name = "localizedNames",
            EmitDefaultValue = false)]
        public Dictionary<string, string> LocalizedNames { get; set; }

        /// <summary>
        /// The OPC UA defined capabilities of the server.
        /// </summary>
        /// <example>LDS</example>
        /// <example>DA</example>
        [DataMember(Name = "capabilities",
            EmitDefaultValue = false)]
        public HashSet<string> Capabilities { get; set; }

        /// <summary>
        /// Discovery urls of the server.
        /// </summary>
        [DataMember(Name = "discoveryUrls",
            EmitDefaultValue = false)]
        public HashSet<string> DiscoveryUrls { get; set; }

        /// <summary>
        /// The discovery profile uri of the server.
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
    }
}
