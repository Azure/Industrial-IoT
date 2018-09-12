// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin {
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Client to access discovery services
    /// </summary>
    public interface IDiscoveryClient {

        /// <summary>
        /// Discover using discovery request.
        /// </summary>
        /// <param name="supervisorId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task DiscoverAsync(string supervisorId,
            DiscoveryRequestModel request);
    }
}
