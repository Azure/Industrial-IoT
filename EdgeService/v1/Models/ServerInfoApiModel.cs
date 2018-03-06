// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.EdgeService.v1.Models {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
    using System.Collections.Generic;

    /// <summary>
    /// Server model for webservice api
    /// </summary>
    public class ServerInfoApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public ServerInfoApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public ServerInfoApiModel(ServerInfoModel model) {
            ApplicationUri = model?.ApplicationUri;
            ApplicationName = model?.ApplicationName;
            ServerCertificate = model?.ServerCertificate;
            Capabilities = model?.Capabilities;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public ServerInfoModel ToServiceModel() {
            return new ServerInfoModel {
                ApplicationUri = ApplicationUri,
                ApplicationName = ApplicationName,
                ServerCertificate = ServerCertificate,
                Capabilities = Capabilities
            };
        }


        /// <summary>
        /// Unique application uri
        /// </summary>
        public string ApplicationUri { get; set; }

        /// <summary>
        /// Name of server
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// Server cert
        /// </summary>
        public byte[] ServerCertificate { get; set; }

        /// <summary>
        /// Capabilities
        /// </summary>
        public List<string> Capabilities { get; set; }
    }
}
