// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Session handle
    /// </summary>
    public interface ISession : IDisposable
    {
        /// <summary>
        /// The server assigned identifier for the current session.
        /// </summary>
        /// <value>The session id.</value>
        NodeId SessionId { get; }

        /// <summary>
        /// The description of the endpoint.
        /// </summary>
        EndpointDescription Endpoint { get; }

        /// <summary>
        /// Gets the endpoint used to connect to the server.
        /// </summary>
        ConfiguredEndpoint ConfiguredEndpoint { get; }

        /// <summary>
        /// Whether a session has beed created with the server.
        /// </summary>
        bool Connected { get; }

        /// <summary>
        /// Gets the period for wich the server will maintain the
        /// session if there is no communication from the client.
        /// </summary>
        TimeSpan SessionTimeout { get; }

        /// <summary>
        /// Returns true if the session is not receiving keep alives.
        /// </summary>
        /// <remarks>
        /// Set to true if the server does not respond for 2 times
        /// the KeepAliveInterval.
        /// Set to false is communication recovers.
        /// </remarks>
        bool KeepAliveStopped { get; }

        /// <summary>
        /// Gets the TickCount in ms of the last keep alive
        /// based on <see cref="HiResClock.TickCount"/>.
        /// </summary>
        int LastKeepAliveTickCount { get; }

        /// <summary>
        /// Gets or set the channel being wrapped by the client object.
        /// </summary>
        /// <value>The transport channel.</value>
        ITransportChannel? NullableTransportChannel { get; }

        /// <summary>
        /// Create or recreate session
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask OpenAsync(CancellationToken ct = default);

        /// <summary>
        /// Reconnects to the server after a network failure
        /// using a waiting connection.
        /// </summary>
        /// <param name="ct"></param>
        ValueTask ReconnectAsync(CancellationToken ct = default);

        /// <summary>
        /// Closes the channel using async call.
        /// </summary>
        /// <param name="closeChannel"></param>
        /// <param name="ct"></param>
        ValueTask<StatusCode> CloseAsync(bool closeChannel,
            CancellationToken ct = default);

        /// <summary>
        /// Detach the channel.
        /// </summary>
        void DetachChannel();
    }
}
