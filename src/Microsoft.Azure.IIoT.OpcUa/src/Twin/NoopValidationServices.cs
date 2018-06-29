// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin {
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.OpcUa;
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// No op validation
    /// </summary>
    public class NoOpValidationServices : IOpcUaValidationServices {

        /// <inheritdoc/>
        public Task<ApplicationRegistrationModel> DiscoverApplicationAsync(
            Uri discoveryUrl) {
            throw new ResourceNotFoundException("Application discovery not supported");
        }

        /// <inheritdoc/>
        public Task<ApplicationRegistrationModel> ValidateEndpointAsync(
            EndpointModel endpoint) {
            throw new ResourceNotFoundException("Application validation not supported");
        }
    }
}
