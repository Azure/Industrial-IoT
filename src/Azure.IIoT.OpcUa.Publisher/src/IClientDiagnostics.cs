// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Control plane to reset client connections and retrieve
    /// diagnostics.
    /// </summary>
    public interface IClientDiagnostics
    {
        /// <summary>
        /// Get all active connections
        /// </summary>
        IReadOnlyList<ConnectionModel> ActiveConnections { get; }

        /// <summary>
        /// Get diagnostic information of all connections
        /// abd return.
        /// </summary>
        /// <returns></returns>
        IReadOnlyList<ConnectionDiagnosticModel> Diagnostics { get; }

        /// <summary>
        /// Reset all connections that are currently running
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task ResetAllClientsAsync(CancellationToken ct = default);

        /// <summary>
        /// Watch diagnostic information of all connections.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        IAsyncEnumerable<ConnectionDiagnosticModel> MonitorAsync(
            CancellationToken ct = default);

        /// <summary>
        /// Get client diagnostics
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        IAsyncEnumerable<ClientDiagnosticsModel> GetDiagnosticsAsync(
            CancellationToken ct = default);
    }
}
