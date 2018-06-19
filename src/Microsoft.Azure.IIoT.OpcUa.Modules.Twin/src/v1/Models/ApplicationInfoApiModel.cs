// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using System.Collections.Generic;

    /// <summary>
    /// Application info model for edge service api
    /// </summary>
    public class ApplicationInfoApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public ApplicationInfoApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public ApplicationInfoApiModel(ApplicationInfoModel model) {
            ApplicationUri = model?.ApplicationUri;
            ApplicationType = model?.ApplicationType ?? ApplicationType.ClientAndServer;
            ApplicationName = model?.ApplicationName;
            ProductUri = model?.ProductUri;
            Certificate = model?.Certificate;
            DiscoveryUrls = model?.DiscoveryUrls;
            DiscoveryProfileUri = model?.DiscoveryProfileUri;
            Capabilities = model?.Capabilities;
            HostAddresses = model?.HostAddresses;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public ApplicationInfoModel ToServiceModel() {
            return new ApplicationInfoModel {
                ApplicationUri = ApplicationUri,
                ApplicationType = ApplicationType,
                ApplicationName = ApplicationName,
                ProductUri = ProductUri,
                Certificate = Certificate,
                HostAddresses = HostAddresses,
                DiscoveryUrls = DiscoveryUrls,
                DiscoveryProfileUri = DiscoveryProfileUri,
                Capabilities = Capabilities
            };
        }

        /// <summary>
        /// Unique application uri
        /// </summary>
        public string ApplicationUri { get; set; }

        /// <summary>
        /// Application type
        /// </summary>
        public ApplicationType ApplicationType { get; set; }

        /// <summary>
        /// Name of server
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// Product uri
        /// </summary>
        public string ProductUri { get; set; }

        /// <summary>
        /// Server cert
        /// </summary>
        public byte[] Certificate { get; set; }

        /// <summary>
        /// Capabilities
        /// </summary>
        public HashSet<string> Capabilities { get; set; }

        /// <summary>
        /// Discovery urls of the server
        /// </summary>
        public HashSet<string> DiscoveryUrls { get; set; }

        /// <summary>
        /// Discovery profile uri
        /// </summary>
        public string DiscoveryProfileUri { get; set; }

        /// <summary>
        /// Host addresses of server application or null
        /// </summary>
        public HashSet<string> HostAddresses { get; set; }
    }
}
