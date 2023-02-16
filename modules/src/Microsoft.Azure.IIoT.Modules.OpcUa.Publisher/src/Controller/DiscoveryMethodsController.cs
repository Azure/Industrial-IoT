// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Controller {
    using Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Filters;
    using Microsoft.Azure.IIoT.Api.Models;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Discovery method controller
    /// </summary>
    [Version("_V1")]
    [Version("_V2")]
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
        public async Task<bool> DiscoverAsync(DiscoveryRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            await _discover.DiscoverAsync(request);
            return true;
        }

        /// <summary>
        /// Cancel discovery
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<bool> CancelAsync(DiscoveryCancelModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            await _discover.CancelAsync(request);
            return true;
        }

        private readonly IDiscoveryServices _discover;
    }
}
