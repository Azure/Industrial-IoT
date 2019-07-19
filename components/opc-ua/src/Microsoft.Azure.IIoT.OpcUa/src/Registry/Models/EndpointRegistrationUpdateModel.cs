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
        /// User authentication to change on the endpoint.
        /// </summary>
        public CredentialModel User { get; set; }

        /// <summary>
        /// Registry operation context
        /// </summary>
        public RegistryOperationContextModel Context { get; set; }
    }
}
