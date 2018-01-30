// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services {
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Internal stack services
    /// </summary>
    public interface IOpcUaValidationServices {

        /// <summary>
        /// Validates and fills out remainder of the server registration
        /// request.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<ServerRegistrationRequestModel> ValidateAsync(
            ServerRegistrationRequestModel request);
    }
}