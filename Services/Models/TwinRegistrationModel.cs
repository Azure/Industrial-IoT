// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models {

    /// <summary>
    /// Twin registration request
    /// </summary>
    public class TwinRegistrationModel {

        /// <summary>
        /// Registered twin identifier
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Server information in the registration
        /// </summary>
        public ServerInfoModel Server { get; set; }

        /// <summary>
        /// Endpoint information in the registration
        /// </summary>
        public EndpointModel Endpoint { get; set; }

        /// <summary>
        /// Whether the registration is out of sync between
        /// client (edge) and server (service) (default: false).
        /// </summary>
        public bool? OutOfSync { get; set; }

        /// <summary>
        /// Whether twin is running on edge
        /// </summary>
        public bool? Connected { get; set; }
    }
}
