// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Extensions;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Client.Subscriptions;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Inputs used to establish one managed session connection.
    /// </summary>
    /// <remarks>
    /// The endpoint is deliberately supplied by the caller. Endpoint discovery remains
    /// owned by the classic client until the managed-session cutover is made.
    /// </remarks>
    internal sealed record class ManagedSessionConnectionRequest
    {
        /// <summary>
        /// Connection identity used as the pool key.
        /// </summary>
        public required ConnectionIdentifier Connection { get; init; }

        /// <summary>
        /// Endpoint selected for the connection.
        /// </summary>
        public required ConfiguredEndpoint Endpoint { get; init; }

        /// <summary>
        /// User identity resolved from the connection credentials.
        /// </summary>
        public IUserIdentity? Identity { get; init; }

        /// <summary>
        /// Preferred locales sent when activating the session.
        /// </summary>
        public ArrayOf<string> PreferredLocales { get; init; }

        /// <summary>
        /// Requested server session lifetime.
        /// </summary>
        public TimeSpan SessionTimeout { get; init; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Maximum duration of an initial connect operation.
        /// </summary>
        public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Optional reverse-connect manager.
        /// </summary>
        public ReverseConnectManager? ReverseConnectManager { get; init; }

        /// <summary>
        /// Reverse-connect server identity.
        /// </summary>
        public Uri? ReverseConnectServerUri { get; init; }
    }

    /// <summary>
    /// Creates managed session connections.
    /// </summary>
    /// <remarks>
    /// This boundary lets tests characterize pool ownership without a server and keeps
    /// managed-session construction independent from the classic client state machine.
    /// </remarks>
    internal interface IManagedSessionProvider
    {
        /// <summary>
        /// Connect a managed session using the supplied request.
        /// </summary>
        Task<IManagedSessionConnection> ConnectAsync(
            ManagedSessionConnectionRequest request, CancellationToken ct);
    }

    /// <summary>
    /// Connection lifetime abstraction around the public managed-session API.
    /// </summary>
    internal interface IManagedSessionConnection : IAsyncDisposable
    {
        /// <summary>
        /// The public session surface exposed by the managed session.
        /// </summary>
        ISession Session { get; }

        /// <summary>
        /// Raised when managed-session connectivity changes.
        /// </summary>
        event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

        /// <summary>
        /// Reconnect the managed session without exposing its implementation type.
        /// </summary>
        Task ReconnectAsync(CancellationToken ct);
    }

    /// <summary>
    /// Adapts a public <see cref="ManagedSession"/> without exposing its internal
    /// subscription implementation.
    /// </summary>
    internal sealed class ManagedSessionConnection : IManagedSessionConnection
    {
        /// <summary>
        /// Create the adapter.
        /// </summary>
        public ManagedSessionConnection(ManagedSession session)
        {
            _session = session ??
                throw new ArgumentNullException(nameof(session));
        }

        /// <inheritdoc/>
        public ISession Session => _session;

        /// <inheritdoc/>
        public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged
        {
            add => _session.ConnectionStateChanged += value;
            remove => _session.ConnectionStateChanged -= value;
        }

        /// <inheritdoc/>
        public ValueTask DisposeAsync()
        {
            return _session.DisposeAsync();
        }

        /// <inheritdoc/>
        public Task ReconnectAsync(CancellationToken ct)
        {
            return _session.ReconnectAsync(null, null, ct);
        }

        private readonly ManagedSession _session;
    }

    /// <summary>
    /// Creates public managed sessions from Publisher connection inputs.
    /// </summary>
    internal sealed class ManagedSessionConnector : IManagedSessionProvider
    {
        /// <summary>
        /// Create a connector.
        /// </summary>
        public ManagedSessionConnector(ApplicationConfiguration configuration,
            ITelemetryContext telemetry, TimeProvider? timeProvider = null)
        {
            _configuration = configuration ??
                throw new ArgumentNullException(nameof(configuration));
            _telemetry = telemetry ??
                throw new ArgumentNullException(nameof(telemetry));
            _timeProvider = timeProvider ??
                TimeProvider.System;
        }

        /// <inheritdoc/>
        public async Task<IManagedSessionConnection> ConnectAsync(
            ManagedSessionConnectionRequest request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.ReverseConnectManager != null &&
                request.ReverseConnectServerUri != null)
            {
                request.Endpoint.ReverseConnect = new ReverseConnectEndpoint
                {
                    Enabled = true,
                    ServerUri = request.ReverseConnectServerUri.ToString()
                };
            }

            var sessionFactory = new DefaultSessionFactory(_telemetry)
            {
                TimeProvider = _timeProvider
            };
            var identity = request.Identity ?? await request.Connection.Connection.User
                .ToUserIdentityAsync(_configuration, ct).ConfigureAwait(false);
            ManagedSession? session = null;
            try
            {
                session = await ManagedSession.CreateAsync(
                    _configuration,
                    request.Endpoint,
                    sessionFactory,
                    identity,
                    telemetry: _telemetry,
                    sessionName: request.Connection.ToString(),
                    sessionTimeout: (uint)request.SessionTimeout.TotalMilliseconds,
                    preferredLocales: request.PreferredLocales,
                    poolNotifications: false,
                    timeProvider: _timeProvider,
                    reverseConnectManager: request.ReverseConnectManager,
                    ct: ct).ConfigureAwait(false);

                // Publisher handlers may retain values after dispatch. Keep pooling disabled
                // until their ownership contract is changed to deep-copy those values.
                if (session.TryGetSubscriptionManager(out ISubscriptionManager? subscriptions))
                {
                    subscriptions.PoolNotifications = false;
                }
                return new ManagedSessionConnection(session);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                if (session != null)
                {
                    try
                    {
                        await session.DisposeAsync().ConfigureAwait(false);
                    }
                    catch (Exception disposeException) when (
                        disposeException is not OutOfMemoryException)
                    {
                    }
                }
                throw;
            }
        }

        private readonly ApplicationConfiguration _configuration;
        private readonly ITelemetryContext _telemetry;
        private readonly TimeProvider _timeProvider;
    }
}
