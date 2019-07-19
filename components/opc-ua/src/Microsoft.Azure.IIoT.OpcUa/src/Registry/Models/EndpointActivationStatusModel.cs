// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {

    /// <summary>
    /// Endpoint activation state
    /// </summary>
    public class EndpointActivationStatusModel {

        /// <summary>
        /// Identifier of the endoint
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Activation state
        /// </summary>
        public EndpointActivationState? ActivationState { get; set; }
    }
}
