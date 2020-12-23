// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Specialized services provided by endpoint identity
    /// </summary>
    public interface ITwinServices {

        /// <summary>
        /// Current state to report
        /// </summary>
        public EndpointConnectivityState State { get; }

        /// <summary>
        /// Called to get endpoint
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<EndpointModel> GetEndpointAsync(CancellationToken ct = default);

        /// <summary>
        /// Called to update endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        Task SetEndpointAsync(EndpointModel endpoint);
    }
}
