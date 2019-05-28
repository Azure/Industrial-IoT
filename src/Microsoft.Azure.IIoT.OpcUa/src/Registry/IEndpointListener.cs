// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Endpoint security services
    /// </summary>
    public interface IEndpointListener {

        /// <summary>
        /// Send the security info of the endpoint
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task OnEndpointAddedAsync(EndpointRegistrationModel model);
    }
}
