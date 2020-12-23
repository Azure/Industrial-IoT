// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using System.Collections.Generic;

    /// <summary>
    /// Application registration update request
    /// </summary>
    public class ApplicationRegistrationUpdateModel {

        /// <summary>
        /// Product uri
        /// </summary>
        public string ProductUri { get; set; }

        /// <summary>
        /// Default name of application
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// Locale of name - defaults to "en"
        /// </summary>
        public string Locale { get; set; }

        /// <summary>
        /// Localized names, keyed on locale.
        /// Set name to null to remove locale.
        /// </summary>
        public Dictionary<string, string> LocalizedNames { get; set; }

        /// <summary>
        /// Application capabilities
        /// </summary>
        public HashSet<string> Capabilities { get; set; }

        /// <summary>
        /// Discovery urls of the application
        /// </summary>
        public HashSet<string> DiscoveryUrls { get; set; }

        /// <summary>
        /// Discovery profile uri
        /// </summary>
        public string DiscoveryProfileUri { get; set; }

        /// <summary>
        /// Gateway server uri
        /// </summary>
        public string GatewayServerUri { get; set; }

        /// <summary>
        /// Operation audit context
        /// </summary>
        public RegistryOperationContextModel Context { get; set; }
    }
}
