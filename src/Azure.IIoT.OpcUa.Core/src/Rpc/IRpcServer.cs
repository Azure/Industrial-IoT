// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Rpc
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// The server listens for request on a connection.
    /// Method servers are registered with the rpc server
    /// and the rpc server will use the first server that
    /// provides a response. It will use the next server
    /// if the method server invocation call throws
    /// a <see cref="NotSupportedException"/>, meaning
    /// server calls are not multiplexed. For this it
    /// might be useful to build a method server multiplexer
    /// implementation instead or register with named
    /// Rpc servers if the host environment supports more
    /// than one running simultaneously.
    /// </summary>
    public interface IRpcServer
    {
        /// <summary>
        /// Name of the technology implementing the rpc
        /// server, e.g., mqtt or kafka.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Get connected method servers
        /// </summary>
        IEnumerable<IRpcHandler> Connected { get; }

        /// <summary>
        /// Connect the server
        /// </summary>
        /// <param name="server"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<IAsyncDisposable> ConnectAsync(IRpcHandler server,
            CancellationToken ct = default);

        /// <summary>
        /// The server must be started after all handlers were
        /// connected.
        /// </summary>
        void Start();
    }
}
