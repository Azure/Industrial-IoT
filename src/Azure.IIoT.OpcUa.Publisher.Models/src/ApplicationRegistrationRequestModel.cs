// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Application information
    /// </summary>
    [DataContract]
    public sealed record class ApplicationRegistrationRequestModel
    {
        /// <summary>
        /// Unique application uri
        /// </summary>
        [DataMember(Name = "applicationUri", Order = 0)]
        [Required]
        public required string ApplicationUri { get; set; }

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
        public string? ProductUri { get; set; }

        /// <summary>
        /// Default name of the server or client.
        /// </summary>
        [DataMember(Name = "applicationName", Order = 3,
            EmitDefaultValue = false)]
        public string? ApplicationName { get; set; }

        /// <summary>
        /// Locale of default name
        /// </summary>
        [DataMember(Name = "locale", Order = 4,
            EmitDefaultValue = false)]
        public string? Locale { get; set; }

        /// <summary>
        /// Site of the application
        /// </summary>
        [DataMember(Name = "siteId", Order = 5,
            EmitDefaultValue = false)]
        public string? SiteId { get; set; }

        /// <summary>
        /// Localized names key off locale id.
        /// </summary>
        [DataMember(Name = "localizedNames", Order = 6,
            EmitDefaultValue = false)]
        public IReadOnlyDictionary<string, string>? LocalizedNames { get; set; }

        /// <summary>
        /// The OPC UA defined capabilities of the server.
        /// </summary>
        /// <example>LDS</example>
        /// <example>DA</example>
        [DataMember(Name = "capabilities", Order = 7,
            EmitDefaultValue = false)]
        public IReadOnlySet<string>? Capabilities { get; set; }

        /// <summary>
        /// Discovery urls of the server.
        /// </summary>
        [DataMember(Name = "discoveryUrls", Order = 8,
            EmitDefaultValue = false)]
        public IReadOnlySet<string>? DiscoveryUrls { get; set; }

        /// <summary>
        /// The discovery profile uri of the server.
        /// </summary>
        [DataMember(Name = "discoveryProfileUri", Order = 9,
            EmitDefaultValue = false)]
        public string? DiscoveryProfileUri { get; set; }

        /// <summary>
        /// Gateway server uri
        /// </summary>
        [DataMember(Name = "gatewayServerUri", Order = 10,
            EmitDefaultValue = false)]
        public string? GatewayServerUri { get; set; }

        /// <summary>
        /// Operation audit context
        /// </summary>
        [DataMember(Name = "context", Order = 11,
            EmitDefaultValue = false)]
        public OperationContextModel? Context { get; set; }
    }
}
