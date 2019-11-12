// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {

    /// <summary>
    /// Server registration request
    /// </summary>
    public class ServerRegistrationRequestModel {

        /// <summary>
        /// Discovery url to use for registration
        /// </summary>
        public string DiscoveryUrl { get; set; }

        /// <summary>
        /// User defined request id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Upon discovery, activate all endpoints with this filter.
        /// </summary>
        public EndpointActivationFilterModel ActivationFilter { get; set; }

        /// <summary>
        /// Operation audit context
        /// </summary>
        public RegistryOperationContextModel Context { get; set; }
    }
}
