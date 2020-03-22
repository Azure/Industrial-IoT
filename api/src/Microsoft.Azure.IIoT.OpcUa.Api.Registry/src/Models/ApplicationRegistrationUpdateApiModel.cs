// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// Application registration update request
    /// </summary>
    [DataContract]
    public class ApplicationRegistrationUpdateApiModel {

        /// <summary>
        /// Product uri
        /// </summary>
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
        /// Locale of default name - defaults to "en"
        /// </summary>
        [DataMember(Name = "locale",
            EmitDefaultValue = false)]
        public string Locale { get; set; }

        /// <summary>
        /// Localized names keyed off locale id.
        /// To remove entry, set value for locale id to null.
        /// </summary>
        [DataMember(Name = "localizedNames",
            EmitDefaultValue = false)]
        public Dictionary<string, string> LocalizedNames { get; set; }

        /// <summary>
        /// Capabilities of the application
        /// </summary>
        [DataMember(Name = "capabilities",
            EmitDefaultValue = false)]
        public HashSet<string> Capabilities { get; set; }

        /// <summary>
        /// Discovery urls of the application
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
    }
}
