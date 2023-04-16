// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Access to clients
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal interface IClientAccessor<T>
    {
        /// <summary>
        /// Get a client handle. The client handle must be
        /// disposed when not used anymore.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="metrics"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<IOpcUaClient> GetOrCreateClientAsync(
            T connection, IMetricsContext? metrics = null,
            CancellationToken ct = default);

        /// <summary>
        /// Get a client handle for a connection or null
        /// if the client does not exist. The session might
        /// be disconnected at point it is returned. The client
        /// handle must be disposed when not used anymore.
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        IOpcUaClient? GetClient(T connection);
    }
}
