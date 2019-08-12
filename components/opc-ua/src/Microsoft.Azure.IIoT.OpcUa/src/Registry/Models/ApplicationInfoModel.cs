// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Application information
    /// </summary>
    public class ApplicationInfoModel {

        /// <summary>
        /// Unique application id
        /// </summary>
        public string ApplicationId { get; set; }

        /// <summary>
        /// Type of application
        /// </summary>
        public ApplicationType ApplicationType { get; set; }

        /// <summary>
        /// Application uri
        /// </summary>
        public string ApplicationUri { get; set; }

        /// <summary>
        /// Product uri
        /// </summary>
        public string ProductUri { get; set; }

        /// <summary>
        /// Default name of application
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// Locale of the application name if not default.
        /// </summary>
        public string Locale { get; set; }

        /// <summary>
        /// Localized Names of application keyed on locale
        /// </summary>
        public Dictionary<string, string> LocalizedNames { get; set; }

        /// <summary>
        /// Application certificate
        /// </summary>
        public byte[] Certificate { get; set; }

        /// <summary>
        /// Application capabilities
        /// </summary>
        public HashSet<string> Capabilities { get; set; }

        /// <summary>
        /// Host addresses of server application or null
        /// </summary>
        public HashSet<string> HostAddresses { get; set; }

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
        /// Site of the application
        /// </summary>
        public string SiteId { get; set; }

        /// <summary>
        /// Supervisor that registered the application
        /// </summary>
        public string SupervisorId { get; set; }

        /// <summary>
        /// Last time application was seen
        /// </summary>
        public DateTime? NotSeenSince { get; set; }

        /// <summary>
        /// Created
        /// </summary>
        public RegistryOperationContextModel Created { get; set; }

        /// <summary>
        /// Updated
        /// </summary>
        public RegistryOperationContextModel Updated { get; set; }
    }
}

