// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Control plane to reset client connections and retrieve
    /// diagnostics.
    /// </summary>
    public interface IClientDiagnostics
    {
        /// <summary>
        /// Reset all connections that are currently running
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task ResetAllClients(CancellationToken ct = default);

        /// <summary>
        /// Set all connections into trace mode for a minute.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task SetTraceModeAsync(CancellationToken ct = default);
    }
}
