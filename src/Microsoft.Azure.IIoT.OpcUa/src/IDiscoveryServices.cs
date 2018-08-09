// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa {
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Application discovery services
    /// </summary>
    public interface IDiscoveryServices {

        /// <summary>
        /// Kick of an application discovery
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task DiscoverAsync(DiscoveryRequestModel request);
    }
}
