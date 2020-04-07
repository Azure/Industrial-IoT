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
        [DataMember(Name = "applicationUri", Order = 0)]
        [Required]
        public string ApplicationUri { get; set; }

        /// <summary>
        /// Type of application
        /// </summary>
        /// <example>Server</example>
        [DataMember(Name = "applicationType", Order = 1,
            EmitDefaultValue = false)]
        public ApplicationType? ApplicationType { get; set; }

        /// <summary>
        /// Product uri of the application.
        /// </summary>
        /// <example>http://contoso.com/fridge/1.0</example>
        [DataMember(Name = "productUri", Order = 2,
            EmitDefaultValue = false)]
        public string ProductUri { get; set; }

        /// <summary>
        /// Default name of the server or client.
        /// </summary>
        [DataMember(Name = "applicationName", Order = 3,
            EmitDefaultValue = false)]
        public string ApplicationName { get; set; }

        /// <summary>
        /// Locale of default name
        /// </summary>
        [DataMember(Name = "locale", Order = 4,
            EmitDefaultValue = false)]
        public string Locale { get; set; }

        /// <summary>
        /// Site of the application
        /// </summary>
        [DataMember(Name = "siteId", Order = 5,
            EmitDefaultValue = false)]
        public string SiteId { get; set; }

        /// <summary>
        /// Localized names key off locale id.
        /// </summary>
        [DataMember(Name = "localizedNames", Order = 6,
            EmitDefaultValue = false)]
        public Dictionary<string, string> LocalizedNames { get; set; }

        /// <summary>
        /// The OPC UA defined capabilities of the server.
        /// </summary>
        /// <example>LDS</example>
        /// <example>DA</example>
        [DataMember(Name = "capabilities", Order = 7,
            EmitDefaultValue = false)]
        public HashSet<string> Capabilities { get; set; }

        /// <summary>
        /// Discovery urls of the server.
        /// </summary>
        [DataMember(Name = "discoveryUrls", Order = 8,
            EmitDefaultValue = false)]
        public HashSet<string> DiscoveryUrls { get; set; }

        /// <summary>
        /// The discovery profile uri of the server.
        /// </summary>
        [DataMember(Name = "discoveryProfileUri", Order = 9,
            EmitDefaultValue = false)]
        public string DiscoveryProfileUri { get; set; }

        /// <summary>
        /// Gateway server uri
        /// </summary>
        [DataMember(Name = "gatewayServerUri", Order = 10,
            EmitDefaultValue = false)]
        public string GatewayServerUri { get; set; }
    }
}
