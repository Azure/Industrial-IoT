// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Enables retrieving status
    /// </summary>
    public interface ISupervisorServices {

        /// <summary>
        /// Get supervisor status
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<SupervisorStatusModel> GetStatusAsync(
            CancellationToken ct = default);

        /// <summary>
        /// Detach inactive endpoint
        /// </summary>
        /// <param name="endpointId"></param>
        /// <returns></returns>
        Task DetachEndpointAsync(string endpointId);

        /// <summary>
        /// Attach endpoint
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="secret"></param>
        /// <returns></returns>
        Task AttachEndpointAsync(string endpointId, string secret);

        /// <summary>
        /// Reset supervisor
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task ResetAsync(CancellationToken ct = default);
    }
}
