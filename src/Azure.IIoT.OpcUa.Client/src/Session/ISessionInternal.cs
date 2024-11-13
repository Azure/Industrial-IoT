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
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Manages a session with a server.
    /// </summary>
    public interface ISessionInternal : ISessionClient
    {
        /// <summary>
        /// Logger of the session
        /// </summary>
        ILoggerFactory LoggerFactory { get; }

        /// <summary>
        /// Raised when a keep alive arrives from the server or an
        /// error is detected.
        /// </summary>
        /// <remarks>
        /// Once a session is created a timer will periodically read
        /// the server state and current time. If this read operation
        /// succeeds this event will be raised each time the keep
        /// alive period elapses.
        /// If an error is detected (KeepAliveStopped == true) then
        /// this event will be raised as well.
        /// </remarks>
        event KeepAliveEventHandler KeepAlive;

        /// <summary>
        /// Raised when an exception occurs while processing a publish response.
        /// </summary>
        /// <remarks>
        /// Exceptions in a publish response are not necessarily fatal and the Session will
        /// attempt to recover by issuing Republish requests if missing messages are detected.
        /// That said, timeout errors may be a symptom of a OperationTimeout that is too short
        /// when compared to the shortest PublishingInterval/KeepAliveCount amount the current
        /// Subscriptions. The OperationTimeout should be twice the minimum value for
        /// PublishingInterval*KeepAliveCount.
        /// </remarks>
        event PublishErrorEventHandler PublishError;

        /// <summary>
        /// Raised when a publish request is about to acknowledge sequence numbers.
        /// </summary>
        /// <remarks>
        /// If the client chose to defer acknowledge of sequence numbers, it is responsible
        /// to transfer these <see cref="SubscriptionAcknowledgement"/> to the deferred list.
        /// </remarks>
        event PublishSequenceNumbersToAcknowledgeEventHandler PublishSequenceNumbersToAcknowledge;

        /// <summary>
        /// Raised to indicate the session configuration changed.
        /// </summary>
        /// <remarks>
        /// An example for a session configuration change is a new user identity,
        /// a new server nonce, a new locale etc.
        /// </remarks>
        event EventHandler SessionConfigurationChanged;

        /// <summary>
        /// Gets the endpoint used to connect to the server.
        /// </summary>
        ConfiguredEndpoint ConfiguredEndpoint { get; }

        /// <summary>
        /// Gets the name assigned to the session.
        /// </summary>
        string SessionName { get; }

        /// <summary>
        /// Gets the period for wich the server will maintain the
        /// session if there is no communication from the client.
        /// </summary>
        TimeSpan SessionTimeout { get; }

        /// <summary>
        /// Gets the local handle assigned to the session.
        /// </summary>
        object? Handle { get; }

        /// <summary>
        /// Gets the user identity currently used for the session.
        /// </summary>
        IUserIdentity Identity { get; }

        /// <summary>
        /// Gets the table of namespace uris known to the server.
        /// </summary>
        NamespaceTable NamespaceUris { get; }

        /// <summary>
        /// Gets the system context for use with the session.
        /// </summary>
        ISystemContext SystemContext { get; }

        /// <summary>
        /// Gets the factory used to create encodeable objects that the server understands.
        /// </summary>
        IEncodeableFactory Factory { get; }

        /// <summary>
        /// Gets the cache of nodes fetched from the server.
        /// </summary>
        INodeCache NodeCache { get; }

        /// <summary>
        /// Gets the subscriptions owned by the session.
        /// </summary>
        IEnumerable<Subscription> Subscriptions { get; }

        /// <summary>
        /// Gets the number of subscriptions owned by the session.
        /// </summary>
        int SubscriptionCount { get; }

        /// <summary>
        /// If the subscriptions are deleted when a session is closed.
        /// </summary>
        bool DeleteSubscriptionsOnClose { get; set; }

        /// <summary>
        /// Gets or Sets how frequently the server is pinged to see if communication is still working.
        /// </summary>
        /// <remarks>
        /// This interval controls how much time elaspes before a communication error is detected.
        /// If everything is ok the KeepAlive event will be raised each time this period elapses.
        /// </remarks>
        TimeSpan KeepAliveInterval { get; set; }

        /// <summary>
        /// Returns true if the session is not receiving keep alives.
        /// </summary>
        /// <remarks>
        /// Set to true if the server does not respond for 2 times the KeepAliveInterval.
        /// Set to false is communication recovers.
        /// </remarks>
        bool KeepAliveStopped { get; }

        /// <summary>
        /// Gets the number of outstanding publish or keep alive requests.
        /// </summary>
        int OutstandingRequestCount { get; }

        /// <summary>
        /// Gets the number of outstanding publish or keep alive requests which appear to be missing.
        /// </summary>
        int DefunctRequestCount { get; }

        /// <summary>
        /// Gets the number of good outstanding publish requests.
        /// </summary>
        int GoodPublishRequestCount { get; }

        /// <summary>
        /// Gets and sets the minimum number of publish requests to be used in the session.
        /// </summary>
        int MinPublishRequestCount { get; set; }

        /// <summary>
        /// Gets and sets the maximum number of publish requests to be used in the session.
        /// </summary>
        int MaxPublishRequestCount { get; set; }

        /// <summary>
        /// Stores the operation limits of a OPC UA Server.
        /// </summary>
        OperationLimits OperationLimits { get; }

        /// <summary>
        /// If the subscriptions are transferred when a session is reconnected.
        /// </summary>
        /// <remarks>
        /// Default <c>false</c>, set to <c>true</c> if subscriptions should
        /// be transferred after reconnect. Service must be supported by server.
        /// </remarks>
        bool TransferSubscriptionsOnRecreate { get; set; }

        /// <summary>
        /// Updates the local copy of the server's namespace uri and server uri tables.
        /// </summary>
        /// <param name="ct">The cancellation token.</param>
        Task FetchNamespaceTablesAsync(CancellationToken ct = default);

        /// <summary>
        /// Adds a subscription to the session.
        /// </summary>
        /// <param name="subscription">The subscription to add.</param>
        bool AddSubscription(Subscription subscription);

        /// <summary>
        /// Removes a transferred subscription from the session.
        /// Called by the session to which the subscription
        /// is transferred to obtain ownership. Internal.
        /// </summary>
        /// <param name="subscription">The subscription to remove.</param>
        bool RemoveTransferredSubscription(Subscription subscription);

        /// <summary>
        /// Removes a subscription from the session.
        /// </summary>
        /// <param name="subscription">The subscription to remove.</param>
        /// <param name="ct">The cancellation token for the request.</param>
        Task<bool> RemoveSubscriptionAsync(Subscription subscription, CancellationToken ct = default);

        /// <summary>
        /// Removes a list of subscriptions from the session.
        /// </summary>
        /// <param name="subscriptions">The list of subscriptions to remove.</param>
        /// <param name="ct">The cancellation token for the request.</param>
        Task<bool> RemoveSubscriptionsAsync(IEnumerable<Subscription> subscriptions, CancellationToken ct = default);

        /// <summary>
        /// Calls the specified method and returns the output arguments.
        /// </summary>
        /// <param name="objectId">The NodeId of the object that provides the method.</param>
        /// <param name="methodId">The NodeId of the method to call.</param>
        /// <param name="ct">The cancellation token for the request.</param>
        /// <param name="args">The input arguments.</param>
        /// <returns>The list of output argument values.</returns>
        Task<IReadOnlyList<object>> CallAsync(NodeId objectId, NodeId methodId, CancellationToken ct = default, params object[] args);

        /// <summary>
        /// Sends an additional publish request.
        /// </summary>
        /// <param name="timeout"></param>
        IAsyncResult? BeginPublish(int timeout);

        /// <summary>
        /// Create the publish requests for the active subscriptions.
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="fullQueue"></param>
        void StartPublishing(int timeout, bool fullQueue);

        /// <summary>
        /// Sends a republish request.
        /// </summary>
        /// <param name="subscriptionId"></param>
        /// <param name="sequenceNumber"></param>
        /// <param name="ct"></param>
        Task<(bool, ServiceResult)> RepublishAsync(uint subscriptionId, uint sequenceNumber, CancellationToken ct = default);
    }
}
