// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Application registration update request
    /// </summary>
    [DataContract]
    public sealed record class ApplicationRegistrationUpdateModel
    {
        /// <summary>
        /// Product uri
        /// </summary>
        [DataMember(Name = "productUri", Order = 0,
            EmitDefaultValue = false)]
        public string? ProductUri { get; set; }

        /// <summary>
        /// Default name of the server or client.
        /// </summary>
        [DataMember(Name = "applicationName", Order = 1,
            EmitDefaultValue = false)]
        public string? ApplicationName { get; set; }

        /// <summary>
        /// Locale of default name - defaults to "en"
        /// </summary>
        [DataMember(Name = "locale", Order = 2,
            EmitDefaultValue = false)]
        public string? Locale { get; set; }

        /// <summary>
        /// Localized names keyed off locale id.
        /// To remove entry, set value for locale id to null.
        /// </summary>
        [DataMember(Name = "localizedNames", Order = 3,
            EmitDefaultValue = false)]
        public Dictionary<string, string>? LocalizedNames { get; set; }

        /// <summary>
        /// Capabilities of the application
        /// </summary>
        [DataMember(Name = "capabilities", Order = 4,
            EmitDefaultValue = false)]
        public HashSet<string>? Capabilities { get; set; }

        /// <summary>
        /// Discovery urls of the application
        /// </summary>
        [DataMember(Name = "discoveryUrls", Order = 5,
            EmitDefaultValue = false)]
        public HashSet<string>? DiscoveryUrls { get; set; }

        /// <summary>
        /// Discovery profile uri
        /// </summary>
        [DataMember(Name = "discoveryProfileUri", Order = 6,
            EmitDefaultValue = false)]
        public string? DiscoveryProfileUri { get; set; }

        /// <summary>
        /// Gateway server uri
        /// </summary>
        [DataMember(Name = "gatewayServerUri", Order = 7,
            EmitDefaultValue = false)]
        public string? GatewayServerUri { get; set; }

        /// <summary>
        /// Operation audit context
        /// </summary>
        [DataMember(Name = "context", Order = 8,
            EmitDefaultValue = false)]
        public OperationContextModel? Context { get; set; }
    }
}
