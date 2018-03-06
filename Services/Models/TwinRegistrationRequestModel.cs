// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models {

    /// <summary>
    /// Twin registration request
    /// </summary>
    public class TwinRegistrationRequestModel {

        /// <summary>
        /// Endpoint information
        /// </summary>
        public EndpointModel Endpoint { get; set; }

        /// <summary>
        /// Desired identifier of the twin
        /// </summary>
        public string Id { get; set; }
    }
}
