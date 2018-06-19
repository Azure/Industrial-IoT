// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa {
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Application and endpoint validation services
    /// </summary>
    public interface IOpcUaValidationServices {

        /// <summary>
        /// Validates endpoint and returns an application
        /// model with the endpoint if found.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        Task<ApplicationRegistrationModel> ValidateEndpointAsync(
            EndpointModel endpoint);

        /// <summary>
        /// Read entire application model from discovery
        /// server using discovery url
        /// </summary>
        /// <param name="discoveryUrl"></param>
        /// <returns></returns>
        Task<ApplicationRegistrationModel> DiscoverApplicationAsync(
            Uri discoveryUrl);
    }
}