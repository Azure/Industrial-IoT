// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using System.Collections.Generic;

    /// <summary>
    /// Application info model for twin module
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
            ApplicationId = model.ApplicationId;
            ApplicationType = model.ApplicationType;
            ApplicationUri = model.ApplicationUri;
            ApplicationName = model.ApplicationName;
            Certificate = model.Certificate;
            ProductUri = model.ProductUri;
            SiteId = model.SiteId;
            HostAddresses = model.HostAddresses;
            SupervisorId = model.SupervisorId;
            DiscoveryProfileUri = model.DiscoveryProfileUri;
            DiscoveryUrls = model.DiscoveryUrls;
            Capabilities = model.Capabilities;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public ApplicationInfoModel ToServiceModel() {
            return new ApplicationInfoModel {
                ApplicationId = ApplicationId,
                ApplicationType = ApplicationType,
                ApplicationUri = ApplicationUri,
                ApplicationName = ApplicationName,
                Certificate = Certificate,
                ProductUri = ProductUri,
                SiteId = SiteId,
                HostAddresses = HostAddresses,
                SupervisorId = SupervisorId,
                DiscoveryProfileUri = DiscoveryProfileUri,
                DiscoveryUrls = DiscoveryUrls,
                Capabilities = Capabilities
            };
        }

        /// <summary>
        /// Unique application id
        /// </summary>
        public string ApplicationId { get; set; }

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

        /// <summary>
        /// Site of the application
        /// </summary>
        public string SiteId { get; set; }

        /// <summary>
        /// Supervisor having registered the application
        /// </summary>
        public string SupervisorId { get; set; }
    }
}
