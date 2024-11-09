/* ========================================================================
 * Copyright (c) 2005-2022 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

namespace Opc.Ua.Client
{
    using System;
    using System.IO;
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
        /// The factory which was used to create the session.
        /// </summary>
        ISessionFactory SessionFactory { get; }

        /// <summary>
        /// Gets the endpoint used to connect to the server.
        /// </summary>
        ConfiguredEndpoint ConfiguredEndpoint { get; }

        /// <summary>
        /// Whether a session has beed created with the server.
        /// </summary>
        /// <value><c>true</c> if connected; otherwise, <c>false</c>.</value>
        bool Connected { get; }

        /// <summary>
        /// Gets the period for wich the server will maintain the
        /// session if there is no communication from the client.
        /// </summary>
        double SessionTimeout { get; }

        /// <summary>
        /// Detach the channel.
        /// </summary>
        void DetachChannel();

        /// <summary>
        /// Gets or set the channel being wrapped by the client object.
        /// </summary>
        /// <value>The transport channel.</value>
        ITransportChannel? NullableTransportChannel { get; }

        /// <summary>
        /// Returns true if the session is not receiving keep alives.
        /// </summary>
        /// <remarks>
        /// Set to true if the server does not respond for 2 times the KeepAliveInterval.
        /// Set to false is communication recovers.
        /// </remarks>
        bool KeepAliveStopped { get; }

        /// <summary>
        /// Gets the TickCount in ms of the last keep alive
        /// based on <see cref="HiResClock.TickCount"/>.
        /// </summary>
        int LastKeepAliveTickCount { get; }

        /// <summary>
        /// Reconnects to the server after a network failure.
        /// </summary>
        /// <param name="ct"></param>
        Task ReconnectAsync(CancellationToken ct = default);

        /// <summary>
        /// Reconnects to the server after a network failure
        /// using a waiting connection.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="ct"></param>
        Task ReconnectAsync(ITransportWaitingConnection connection,
            CancellationToken ct = default);

        /// <summary>
        /// Closes the channel using async call.
        /// </summary>
        /// <param name="ct"></param>
        Task<StatusCode> CloseAsync(CancellationToken ct = default);
    }
}
