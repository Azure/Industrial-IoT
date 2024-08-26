// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Models
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Services;
    using System;
    using System.Diagnostics;
    using System.Threading;

    /// <summary>
    /// Context for service call invocations
    /// </summary>
    public sealed record class ServiceCallContext : ISessionHandle
    {
        /// <inheritdoc/>
        public IOpcUaSession Session { get; }

        /// <inheritdoc/>
        public TimeSpan ServiceCallTimeout { get; }

        /// <summary>
        /// A continuation token to track after
        /// returning from the call.
        /// </summary>
        public string? TrackedToken { get; set; }

        /// <summary>
        /// A token to release from tracking after
        /// returning from the call.
        /// </summary>
        public string? UntrackedToken { get; set; }

        /// <summary>
        /// Cancel any calls on this token
        /// </summary>
        public CancellationToken Ct { get; }

        /// <summary>
        /// Create context
        /// </summary>
        /// <param name="session"></param>
        /// <param name="serviceCallTimeout"></param>
        /// <param name="ct"></param>
        internal ServiceCallContext(IOpcUaSession session,
            TimeSpan serviceCallTimeout, CancellationToken ct = default)
        {
            Session = session;
            ServiceCallTimeout = serviceCallTimeout;
            Ct = ct;
        }

        /// <summary>
        /// Create context
        /// </summary>
        /// <param name="session"></param>
        /// <param name="serviceCallTimeout"></param>
        /// <param name="client"></param>
        /// <param name="sessionLock"></param>
        /// <param name="ct"></param>
        internal ServiceCallContext(IOpcUaSession session,
            TimeSpan serviceCallTimeout, OpcUaClient client,
            IDisposable sessionLock, CancellationToken ct = default)
            : this(session, serviceCallTimeout, ct)
        {
            client.AddRef();

            _client = client;
            _sessionLock = sessionLock;
            // TODO: we could timeout and dispose to catch leaks
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_client != null)
            {
                Debug.Assert(_sessionLock != null);
                _sessionLock.Dispose();
                _client.Release();
                _client = null;
            }
        }

        private OpcUaClient? _client;
        private readonly IDisposable? _sessionLock;
    }
}
