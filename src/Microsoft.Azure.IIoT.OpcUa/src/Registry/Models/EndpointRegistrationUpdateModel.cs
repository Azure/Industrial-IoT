// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {

    /// <summary>
    /// Endpoint registration update request
    /// </summary>
    public class EndpointRegistrationUpdateModel {

        /// <summary>
        /// Identifier of the endpoint to update
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// User authentication to change on the endpoint.
        /// </summary>
        public CredentialModel User { get; set; }
    }
}
