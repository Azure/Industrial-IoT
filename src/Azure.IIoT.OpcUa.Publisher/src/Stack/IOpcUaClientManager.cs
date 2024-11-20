// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Connectivity state event
    /// </summary>
    public sealed class EndpointConnectivityStateEventArgs : EventArgs
    {
        /// <summary>
        /// State
        /// </summary>
        public EndpointConnectivityState State { get; }

        internal EndpointConnectivityStateEventArgs(EndpointConnectivityState state)
        {
            State = state;
        }
    }

    /// <summary>
    /// Client managers manages clients connected to servers and provides
    /// access to session services.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IOpcUaClientManager<T>
    {
        /// <summary>
        /// Connectivity state change events
        /// </summary>
        event EventHandler<EndpointConnectivityStateEventArgs> OnConnectionStateChange;

        /// <summary>
        /// Acquire a session which will be usable until disposed.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="header"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ISessionHandle> AcquireSessionAsync(T connection,
            RequestHeaderModel? header = null, CancellationToken ct = default);

        /// <summary>
        /// Execute the service on the provided session and
        /// return the result.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="connection"></param>
        /// <param name="func"></param>
        /// <param name="header"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<TResult> ExecuteAsync<TResult>(T connection,
            Func<ServiceCallContext, Task<TResult>> func,
            RequestHeaderModel? header = null, CancellationToken ct = default);

        /// <summary>
        /// Execute the functions from stack on the provided
        /// session and stream the results.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="connection"></param>
        /// <param name="operation"></param>
        /// <param name="header"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        IAsyncEnumerable<TResult> ExecuteAsync<TResult>(T connection,
            AsyncEnumerableBase<TResult> operation, RequestHeaderModel? header = null,
            CancellationToken ct = default);

        /// <summary>
        /// Create new subscription with the subscription configuration.
        /// The callback will have been called with the new subscription
        /// which then can be used to manage the subscription.
        /// </summary>
        /// <param name="connection">The connection to use</param>
        /// <param name="subscription">The subscription template</param>
        /// <param name="callback">Callbacks from the subscription</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<ISubscriptionRegistration> CreateSubscriptionAsync(T connection,
            SubscriptionModel subscription, ISubscriber callback,
            CancellationToken ct = default);
    }
}
