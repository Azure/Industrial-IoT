// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.Discovery.Controllers {
    using Microsoft.Azure.IIoT.Modules.Discovery.Filters;
    using Microsoft.Azure.IIoT.Modules.Discovery.Models;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Discovery method controller
    /// </summary>
    [Version(1)]
    [Version(2)]
    [ExceptionsFilter]
    public class DiscoveryMethodsController : IMethodController {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="discover"></param>
        public DiscoveryMethodsController(IDiscoveryServices discover) {
            _discover = discover ?? throw new ArgumentNullException(nameof(discover));
        }

        /// <summary>
        /// Discover application
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<bool> DiscoverAsync(DiscoveryRequestInternalApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            await _discover.DiscoverAsync(request.ToServiceModel());
            return true;
        }

        /// <summary>
        /// Cancel discovery
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<bool> CancelAsync(DiscoveryCancelInternalApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            await _discover.CancelAsync(request.ToServiceModel());
            return true;
        }

        private readonly IDiscoveryServices _discover;
    }
}
