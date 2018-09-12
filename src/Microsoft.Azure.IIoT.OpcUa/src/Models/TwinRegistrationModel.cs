// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Models {

    /// <summary>
    /// Twin registration
    /// </summary>
    public class TwinRegistrationModel {

        /// <summary>
        /// Twin and therefore endpoint identifier
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Site of endpoint
        /// </summary>
        public string SiteId { get; set; }

        /// <summary>
        /// Endpoint information in the registration
        /// </summary>
        public EndpointModel Endpoint { get; set; }

        /// <summary>
        /// Security level of the endpoint
        /// </summary>
        public int? SecurityLevel { get; set; }

        /// <summary>
        /// Endpoint cert that was registered.
        /// </summary>
        public byte[] Certificate { get; set; }

        /// <summary>
        /// Supervisor that registered the twin.
        /// </summary>
        public string SupervisorId { get; set; }
    }
}
