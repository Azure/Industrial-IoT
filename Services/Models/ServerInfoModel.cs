// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models {
    using System.Collections.Generic;

    /// <summary>
    /// Server information
    /// </summary>
    public class ServerInfoModel {

        /// <summary>
        /// Unique server id
        /// </summary>
        public string ServerId { get; set; }

        /// <summary>
        /// Application uri
        /// </summary>
        public string ApplicationUri { get; set; }

        /// <summary>
        /// Edge supervisor that owns this endpoint
        /// </summary>
        public string SupervisorId { get; set; }

        /// <summary>
        /// Application name of server
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
