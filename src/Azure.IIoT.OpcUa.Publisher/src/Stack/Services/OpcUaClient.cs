// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Exceptions;
    using Furly.Extensions.Serializers;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Nito.AsyncEx;
    using Opc.Ua;
    using Opc.Ua.Bindings;
    using Opc.Ua.Client;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Metrics;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Runtime.CompilerServices;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;

    /// <summary>
    /// OPC UA Client based on official ua client reference sample.
    /// </summary>
    internal sealed partial class OpcUaClient : IOpcUaClientDiagnostics, IDisposable
    {
        /// <summary>
        /// Client namespace
        /// </summary>
        internal const string Namespace = "http://opcfoundation.org/UA/Client/Types.xsd";

        /// <summary>
        /// The session keepalive interval to be used in ms.
        /// </summary>
        public TimeSpan? KeepAliveInterval { get; set; }

        /// <summary>
        /// Min time to wait until reconnect.
        /// </summary>
        public TimeSpan? MinReconnectDelay { get; set; }

        /// <summary>
        /// Timeout for session creation
        /// </summary>
        public TimeSpan? CreateSessionTimeout { get; set; }

        /// <summary>
        /// The session lifetime.
        /// </summary>
        public TimeSpan? SessionTimeout { get; set; }

        /// <summary>
        /// Session operation timeout
        /// </summary>
        public TimeSpan? OperationTimeout { get; set; }

        /// <summary>
        /// Service call timeout
        /// </summary>
        public TimeSpan? ServiceCallTimeout { get; set; }

        /// <summary>
        /// Connect timeout for service calls
        /// </summary>
        public TimeSpan? ConnectTimeout { get; set; }

        /// <summary>
        /// Minimum number of publish requests to queue
        /// </summary>
        public int? MinPublishRequests { get; set; }

        /// <summary>
        /// Max number of publish requests to ever queue
        /// </summary>
        public int? MaxPublishRequests { get; set; }

        /// <summary>
        /// Percentage ratio of publish requests per subscription
        /// </summary>
        public int? PublishRequestsPerSubscriptionPercent { get; set; }

        /// <summary>
        /// The linger timeout.
        /// </summary>
        public TimeSpan? LingerTimeout { get; set; }

        /// <summary>
        /// Disable complex type preloading.
        /// </summary>
        public bool DisableComplexTypePreloading { get; set; }

        /// <summary>
        /// Operation limits to use in the sessions
        /// </summary>
        internal Opc.Ua.Client.Limits? LimitOverrides { get; set; }

        /// <summary>
        /// Last diagnostic information on this client
        /// </summary>
        internal ChannelDiagnosticModel LastDiagnostics { get; private set; }

        /// <summary>
        /// No complex type loading ever
        /// </summary>
        public bool DisableComplexTypeLoading
            => _connection.Options.HasFlag(ConnectionOptions.NoComplexTypeSystem);

        /// <summary>
        /// Transfer subscription on reconnect
        /// </summary>
        public bool DisableTransferSubscriptionOnReconnect
            => _connection.Options.HasFlag(ConnectionOptions.NoSubscriptionTransfer);

        /// <summary>
        /// Dump diagnostics for this client
        /// </summary>
        public bool DumpDiagnostics
            => _connection.Options.HasFlag(ConnectionOptions.DumpDiagnostics);

        /// <summary>
        /// Client is connected
        /// </summary>
        public bool IsConnected
            => _session?.Connected ?? false;

        /// <inheritdoc/>
        public EndpointConnectivityState State { get; private set; }

        /// <inheritdoc/>
        public int BadPublishRequestCount
            => _session?.Subscriptions.BadPublishRequestCount ?? 0;

        /// <inheritdoc/>
        public int GoodPublishRequestCount
            => _session?.Subscriptions.GoodPublishRequestCount ?? 0;

        /// <inheritdoc/>
        public int PublishWorkerCount
            => _session?.Subscriptions.PublishWorkerCount ?? 0;

        /// <inheritdoc/>
        public int SubscriptionCount
            => _session?.Subscriptions.Items.Count(s => s.Created) ?? 0;

        /// <inheritdoc/>
        public int MinPublishRequestCount
            => _session?.Subscriptions.MinPublishWorkerCount ?? 0;

        /// <inheritdoc/>
        public int ReconnectCount { get; private set; }

        /// <inheritdoc/>
        public int ConnectCount { get; private set; }

        /// <summary>
        /// Disconnected state
        /// </summary>
        internal static IOpcUaClientDiagnostics Disconnected { get; }
            = new DisconnectState();

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="connection"></param>
        /// <param name="serializer"></param>
        /// <param name="observability"></param>
        /// <param name="metrics"></param>
        /// <param name="notifier"></param>
        /// <param name="reverseConnectManager"></param>
        /// <param name="diagnosticsCallback"></param>
        /// <param name="options"></param>
        /// <param name="subscriptionOptions"></param>
        /// <param name="sessionName"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public OpcUaClient(ApplicationConfiguration configuration,
            ConnectionIdentifier connection, IJsonSerializer serializer,
            IObservability observability, IMetricsContext metrics,
            EventHandler<EndpointConnectivityStateEventArgs>? notifier,
            ReverseConnectManager? reverseConnectManager,
            Action<ChannelDiagnosticModel> diagnosticsCallback,
            IOptions<OpcUaClientOptions> options,
            IOptions<OpcUaSubscriptionOptions> subscriptionOptions,
            string? sessionName = null)
        {
            _observability = observability;
            if (connection?.Connection?.Endpoint?.Url == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            _options = options;
            _subscriptionOptions = subscriptionOptions;
            _connection = connection.Connection;
            _diagnosticsCb = diagnosticsCallback;
            LastDiagnostics = new ChannelDiagnosticModel
            {
                Connection = _connection,
                TimeStamp = _observability.TimeProvider.GetUtcNow()
            };
            Debug.Assert(_connection.GetEndpointUrls().Any());
            _reverseConnectManager = reverseConnectManager;

            _metrics = metrics;
            _configuration = configuration;
            _serializer = serializer;
            _notifier = notifier;

            _meter = observability.MeterFactory.Create(nameof(OpcUaClient));
            InitializeMetrics();

            _logger = _observability.LoggerFactory.CreateLogger<OpcUaClient>();
            _tokens = new Dictionary<string, CancellationTokenSource>();
            State = EndpointConnectivityState.Disconnected;
            _sessionName = sessionName ?? connection.ToString();

            OperationTimeout = _options.Value.Quotas.OperationTimeout == 0 ? null :
                TimeSpan.FromMilliseconds(_options.Value.Quotas.OperationTimeout);
            DisableComplexTypePreloading =
                _options.Value.DisableComplexTypePreloading ?? false;
            MinReconnectDelay =
                _options.Value.MinReconnectDelayDuration;
            CreateSessionTimeout =
                _options.Value.CreateSessionTimeoutDuration;
            KeepAliveInterval =
                _options.Value.KeepAliveIntervalDuration;
            ServiceCallTimeout =
                _options.Value.DefaultServiceCallTimeoutDuration;
            ConnectTimeout =
                _options.Value.DefaultConnectTimeoutDuration;
            SessionTimeout =
                _options.Value.DefaultSessionTimeoutDuration;
            LingerTimeout =
                _options.Value.LingerTimeoutDuration;
            LimitOverrides
                = new Opc.Ua.Client.Limits
                {
                    MaxNodesPerRead =
                        (uint)(_options.Value.MaxNodesPerReadOverride ?? 0),
                    MaxNodesPerBrowse =
                        (uint)(_options.Value.MaxNodesPerBrowseOverride ?? 0)
                    // ...
                };
            MinPublishRequests =
                _options.Value.MinPublishRequests;
            MaxPublishRequests =
                _options.Value.MaxPublishRequests;
            PublishRequestsPerSubscriptionPercent =
                _options.Value.PublishRequestsPerSubscriptionPercent;
            _maxReconnectPeriod =
                options.Value.MaxReconnectDelayDuration ?? TimeSpan.Zero;
            if (_maxReconnectPeriod == TimeSpan.Zero)
            {
                _maxReconnectPeriod = TimeSpan.FromSeconds(30);
            }
            _reconnectHandler = new SessionReconnectHandler(_observability, true,
                (int)_maxReconnectPeriod.TotalMilliseconds);
            _cts = new CancellationTokenSource();
            _channel = Channel.CreateUnbounded<(ConnectionEvent, object?)>();
            _disconnectLock = _lock.WriterLock(_cts.Token);
            _channelMonitor = _observability.TimeProvider.CreateTimer(
                _ => OnUpdateConnectionDiagnostics(),
                null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _resyncTimer = _observability.TimeProvider.CreateTimer(
                _ => TriggerSubscriptionSynchronization(),
                null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _diagnosticsDumper = !DumpDiagnostics ? null :
                DumpDiagnosticsPeriodicallyAsync(_cts.Token);
            _sessionManager = ManageSessionStateMachineAsync(_cts.Token);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Release();
        }

        /// <inheritdoc/>
        public override string? ToString()
        {
            return $"{_sessionName} [state:{State}|refs:{_refCount}]";
        }

        /// <summary>
        /// Reset the client
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal Task ResetAsync(CancellationToken ct)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            try
            {
                ct.Register(() => tcs.TrySetCanceled());
                _logger.LogDebug("{Client}: Resetting...", this);
                TriggerConnectionEvent(ConnectionEvent.Reset, tcs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Client}: Failed to reset.", this);
                tcs.TrySetException(ex);
            }
            return tcs.Task;
        }

        /// <summary>
        /// Get session diagnostics
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal async Task<SessionDiagnosticsModel?> GetSessionDiagnosticsAsync(
            CancellationToken ct = default)
        {
            if (_session?.Connected == true)
            {
                return await _session.GetServerDiagnosticAsync(ct).ConfigureAwait(false);
            }
            return null;
        }

        /// <summary>
        /// Close client
        /// </summary>
        /// <param name="shutdown"></param>
        /// <returns></returns>
        internal async ValueTask CloseAsync(bool shutdown = false)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            try
            {
                _disposed = true;

                _logger.LogDebug("{Client}: Closing...", this);
                await _cts.CancelAsync().ConfigureAwait(false);

                await _sessionManager.ConfigureAwait(false);
                _reconnectHandler.Dispose();

                foreach (var sampler in _samplers.Values)
                {
                    await sampler.DisposeAsync().ConfigureAwait(false);
                }

                _samplers.Clear();

                foreach (var browser in _browsers.Values)
                {
                    await browser.DisposeAsync().ConfigureAwait(false);
                }

                _browsers.Clear();

                await CloseSessionAsync(shutdown).ConfigureAwait(false);

                State = EndpointConnectivityState.Disconnected;

                if (_diagnosticsDumper != null)
                {
                    await _diagnosticsDumper.ConfigureAwait(false);
                }

                _logger.LogInformation("{Client}: Successfully closed.", this);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Client}: Failed to close.", this);
            }
            finally
            {
                _channelMonitor.Dispose();
                _resyncTimer.Dispose();
                _cts.Dispose();
                _subscriptionLock.Dispose();
                _meter.Dispose();
            }
        }

        /// <summary>
        /// Acquire a session
        /// </summary>
        /// <param name="connectTimeout"></param>
        /// <param name="serviceCallTimeout"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ConnectionException"></exception>
        /// <exception cref="TimeoutException"></exception>
        internal async Task<ISessionHandle> AcquireAsync(int? connectTimeout,
            int? serviceCallTimeout, CancellationToken cancellationToken)
        {
            var timeout = GetConnectCallTimeout(connectTimeout, serviceCallTimeout);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var ct = cts.Token;
            cts.CancelAfter(timeout); // wait max timeout on the reader lock/session
            while (true)
            {
                if (_disposed)
                {
                    throw new ConnectionException($"Session {_sessionName} was closed.");
                }
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var readerlock = await _lock.ReaderLockAsync(ct).ConfigureAwait(false);
                    try
                    {
                        if (_session != null)
                        {
                            if (!DisableComplexTypeLoading && !_session.IsTypeSystemLoaded)
                            {
                                // Ensure type system is loaded
                                cts.CancelAfter(timeout);
                                await _session.GetComplexTypeSystemAsync(ct).ConfigureAwait(false);
                            }

                            //
                            // Now clients can continue the operation with the session handle
                            // which encapsulates the release of the reader lock as well as
                            // the ref count to the client.
                            //
                            var sessionLock = readerlock;
                            readerlock = null; // Do not dispose below but when handle is disposed
                            return new ServiceCallContext(_session, GetServiceCallTimeout(
                                serviceCallTimeout), this, sessionLock, cancellationToken);
                        }
                    }
                    finally
                    {
                        readerlock?.Dispose();
                    }
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException("Connecting to the endpoint timed out.");
                }
            }
        }

        /// <summary>
        /// Safely invoke the service call and retry if the session
        /// disconnected during call.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="service"></param>
        /// <param name="connectTimeout"></param>
        /// <param name="serviceCallTimeout"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ConnectionException"></exception>
        /// <exception cref="TimeoutException"></exception>
        internal async Task<T> RunAsync<T>(Func<ServiceCallContext, Task<T>> service,
            int? connectTimeout, int? serviceCallTimeout, CancellationToken cancellationToken)
        {
            var timeout = GetConnectCallTimeout(connectTimeout, serviceCallTimeout);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var ct = cts.Token;
            cts.CancelAfter(timeout); // wait max timeout on the reader lock/session
            while (true)
            {
                if (_disposed)
                {
                    throw new ConnectionException($"Session {_sessionName} was closed.");
                }
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    using var readerlock = await _lock.ReaderLockAsync(ct).ConfigureAwait(false);
                    if (_session != null)
                    {
                        if (!DisableComplexTypeLoading && !_session.IsTypeSystemLoaded)
                        {
                            // Ensure type system is loaded
                            cts.CancelAfter(timeout);
                            await _session.GetComplexTypeSystemAsync(ct).ConfigureAwait(false);
                        }

                        var serviceTimeout = GetServiceCallTimeout(serviceCallTimeout);
                        using var context = new ServiceCallContext(_session, serviceTimeout, ct: ct);
                        cts.CancelAfter(serviceTimeout);
                        var result = await service(context).ConfigureAwait(false);

                        //
                        // Check wether tracked and untracked token are the same. This is the case
                        // with kepserver, which uses the same token for all continuations. If it
                        // is the same, it is already tracked. If it is different, we need to untrack
                        // and track the new one
                        //
                        if (context.TrackedToken != null && context.TrackedToken != context.UntrackedToken)
                        {
                            AddRef(context.TrackedToken);
                        }
                        else if (LingerTimeout != null)
                        {
                            AddRef(_sessionName, LingerTimeout);
                        }
                        if (context.UntrackedToken != null && context.TrackedToken != context.UntrackedToken)
                        {
                            Release(context.UntrackedToken);
                        }
                        return result;
                    }
                    // We are not resetting the timeout here since we have not yet been
                    // able to obtain a session in the current timeout.
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException(
                        "Connecting to the endpoint or the request itself timed out.");
                }
                catch (Exception ex) when (!IsConnected && !cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("{Client}: Session disconnected during service call " +
                        "with message {Message}, retrying.", this, ex.Message);

                    cts.CancelAfter(timeout); // Reset timeout again to wait again for session
                }
            }
        }

        /// <summary>
        /// Safely invoke a streaming service and retry if the session
        /// disconnected during an operation.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="operation"></param>
        /// <param name="connectTimeout"></param>
        /// <param name="serviceCallTimeout"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ConnectionException"></exception>
        /// <exception cref="TimeoutException"></exception>
        internal async IAsyncEnumerable<T> RunAsync<T>(AsyncEnumerableBase<T> operation,
            int? connectTimeout, int? serviceCallTimeout,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var timeout = GetConnectCallTimeout(connectTimeout, serviceCallTimeout);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var ct = cts.Token;
            cts.CancelAfter(timeout); // wait max timeout on the reader lock/session
            operation.Reset();
            while (operation.HasMore)
            {
                if (_disposed)
                {
                    throw new ConnectionException($"Session {_sessionName} was closed.");
                }
                cancellationToken.ThrowIfCancellationRequested();
                if (_disposed)
                {
                    throw new ConnectionException($"Session {_sessionName} was closed.");
                }
                IEnumerable<T> results;
                try
                {
                    using var readerlock = await _lock.ReaderLockAsync(ct).ConfigureAwait(false);
                    if (_session != null)
                    {
                        // Ensure type system is loaded
                        if (!DisableComplexTypeLoading && !_session.IsTypeSystemLoaded)
                        {
                            cts.CancelAfter(timeout);
                            await _session.GetComplexTypeSystemAsync(ct).ConfigureAwait(false);
                        }

                        var serviceTimeout = GetServiceCallTimeout(serviceCallTimeout);
                        using var context = new ServiceCallContext(_session, serviceTimeout, ct: ct);
                        cts.CancelAfter(serviceTimeout);
                        results = await operation.ExecuteAsync(context).ConfigureAwait(false);
                    }
                    else
                    {
                        // We are not resetting the timeout here since we have not yet been
                        // able to obtain a session in the current timeout.
                        continue;
                    }
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException(
                        "Connecting to the endpoint or a request operation timed out.");
                }
                catch (Exception ex) when (!IsConnected && !cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("{Client}: Session disconnected during service call " +
                        "with message {Message}, retrying.", this, ex.Message);

                    cts.CancelAfter(timeout); // Reset timeout again to wait again for session
                    continue;
                }

                foreach (var result in results)
                {
                    yield return result;
                }
                cts.CancelAfter(timeout); // Reset timeout now to wait max timeout for session
            }
        }

        /// <summary>
        /// Connect
        /// </summary>
        /// <param name="token"></param>
        /// <param name="expiresAfter"></param>
        /// <returns></returns>
        internal void AddRef(string? token = null, TimeSpan? expiresAfter = null)
        {
            // If token provided create a registered release
            CancellationTokenSource? cts = null;
            if (token != null)
            {
                lock (_tokens)
                {
                    //
                    // Get any registered token and see if we can just re-arm
                    // the timer (when the cancellation has not been requested
                    // yet. If so we do not need to add ref at all and just
                    // re-use the existing registered token. If there is a
                    // token id collision then there is the potential that
                    // tokens get removed too early using the release(token)
                    // api. Since that is only used in the case of continuation
                    // tokens we expect that not to happen on the same session.
                    //
                    if (_tokens.TryGetValue(token, out cts))
                    {
                        cts.CancelAfter(expiresAfter ?? TimeSpan.FromSeconds(10));

                        if (!cts.IsCancellationRequested)
                        {
                            // Re-armed the current timer no need to add ref
                            return;
                        }

                        _tokens.Remove(token);
                    }

                    cts = new CancellationTokenSource();
                    _tokens.Add(token, cts);
                }
            }

            if (Interlocked.Increment(ref _refCount) == 1)
            {
                // Post connection request
                TriggerConnectionEvent(ConnectionEvent.Connect);
            }

            if (cts != null)
            {
                //
                // Now that we took a reference register callback that removes
                // registered token source under lock if it is the current one
                // and then cancel and call release. After release dispose the
                // token source to free the timer.
                //
                cts.Token.Register(() =>
                {
                    Debug.Assert(token != null, "Captured token should not be null");
                    lock (_tokens)
                    {
                        if (_tokens.TryGetValue(token, out var registered) &&
                            registered == cts)
                        {
                            _tokens.Remove(token);
                        }
                    }

                    Release();
                    cts.Dispose();
                });
                //
                // Start the cancellation token source timer now and
                // take a new reference.
                //
                cts.CancelAfter(expiresAfter ?? TimeSpan.FromSeconds(10));
            }
        }

        /// <summary>
        /// Disconnect
        /// </summary>
        /// <param name="token"></param>
        internal void Release(string? token = null)
        {
            if (token == null)
            {
                // Decrement reference count
                if (Interlocked.Decrement(ref _refCount) == 0)
                {
                    // Post disconnect request
                    TriggerConnectionEvent(ConnectionEvent.Disconnect);
                }
            }
            else if (_tokens.TryGetValue(token, out var cts))
            {
                //
                // Cancel will callback back into here, unregister from
                // tokens cache, release the reference (above) and then
                // dispose the tracked cancellation token source.
                //
                cts.Cancel();
            }

            // No token so we either expired or never registered
            // the first is expected, the second is a bug, we cannot
            // tell the difference though.
        }

        /// <summary>
        /// Manages the underlying session state machine.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task ManageSessionStateMachineAsync(CancellationToken ct)
        {
            var currentSessionState = SessionState.Disconnected;

            var reconnectPeriod = 0;
            var reconnectTimer = _observability.TimeProvider.CreateTimer(
                _ => TriggerConnectionEvent(ConnectionEvent.ConnectRetry), null,
                Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            try
            {
                await using (reconnectTimer.ConfigureAwait(false))
                {
                    try
                    {
                        await foreach (var (trigger, context) in
                            _channel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
                        {
                            _logger.LogDebug("{Client}: Processing event {Event} in State {State}...",
                                this, trigger, currentSessionState);

                            switch (trigger)
                            {
                                case ConnectionEvent.Reset:
                                    if (currentSessionState != SessionState.Connected)
                                    {
                                        (context as TaskCompletionSource)?.TrySetResult();
                                        break;
                                    }
                                    // If currently reconnecting, dispose the reconnect handler
                                    _reconnectHandler.CancelReconnect();
                                    //
                                    // Close bypassing everything but keep channel open then trigger a
                                    // reconnect. The reconnect will recreate the session and subscriptions
                                    //
                                    Debug.Assert(_session != null);
                                    await _session.CloseAsync(false, default).ConfigureAwait(false);
                                    goto case ConnectionEvent.StartReconnect;
                                case ConnectionEvent.Connect:
                                    if (currentSessionState == SessionState.Disconnected)
                                    {
                                        // Start connecting
                                        reconnectTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                                        currentSessionState = SessionState.Connecting;
                                        reconnectPeriod = GetMinReconnectPeriod();
                                    }
                                    goto case ConnectionEvent.ConnectRetry;
                                case ConnectionEvent.ConnectRetry:
                                    switch (currentSessionState)
                                    {
                                        case SessionState.Connecting:
                                            Debug.Assert(_reconnectHandler.State == SessionReconnectHandler.ReconnectState.Ready);
                                            Debug.Assert(_disconnectLock != null);
                                            Debug.Assert(_session == null);

                                            if (!await TryConnectAsync(ct).ConfigureAwait(false))
                                            {
                                                // Reschedule connecting
                                                Debug.Assert(reconnectPeriod != 0, "Reconnect period should not be 0.");
                                                var retryDelay = TimeSpan.FromMilliseconds(
                                                    _reconnectHandler.CheckedReconnectPeriod(reconnectPeriod));
                                                _logger.LogInformation("{Client}: Retrying connecting session in {RetryDelay}...",
                                                    this, retryDelay);
                                                reconnectTimer.Change(retryDelay, Timeout.InfiniteTimeSpan);
                                                reconnectPeriod = _reconnectHandler.JitteredReconnectPeriod(reconnectPeriod);
                                                break;
                                            }

                                            Debug.Assert(_session != null);
                                            // Sync subscriptions
                                            await SyncAsync(true, ct).ConfigureAwait(false);

                                            // Allow access to session now
                                            Debug.Assert(_disconnectLock != null);
                                            _disconnectLock.Dispose();
                                            _disconnectLock = null;

                                            currentSessionState = SessionState.Connected;
                                            NotifySubscriptions(_session, false);
                                            break;
                                        case SessionState.Disconnected:
                                        case SessionState.Connected:
                                            // Nothing to do, already disconnected or connected
                                            break;
                                        case SessionState.Reconnecting:
                                            Debug.Fail("Should not be connecting during reconnecting.");
                                            break;
                                    }
                                    break;
                                case ConnectionEvent.SubscriptionSyncOne:
                                    var subscriptionToSync = context as VirtualSubscription;
                                    Debug.Assert(subscriptionToSync != null);
                                    await SyncAsync(subscriptionToSync, ct).ConfigureAwait(false);
                                    break;
                                case ConnectionEvent.SubscriptionSyncAll:
                                    await SyncAsync(false, ct).ConfigureAwait(false);
                                    break;
                                case ConnectionEvent.StartReconnect: // sent by the keep alive timeout path
                                    switch (currentSessionState)
                                    {
                                        case SessionState.Connected: // only valid when connected.
                                            Debug.Assert(_reconnectHandler.State == SessionReconnectHandler.ReconnectState.Ready);

                                            // Ensure no more access to the session through reader locks
                                            Debug.Assert(_disconnectLock == null);
                                            _disconnectLock = await _lock.WriterLockAsync(ct);

                                            _logger.LogInformation("{Client}: Reconnecting session {Session} due to {Reason}...",
                                                this, _sessionName, (context is ServiceResult sr) ? "error " + sr : "RESET");
                                            Debug.Assert(_session != null);
                                            var state = _reconnectHandler.BeginReconnect(_session,
                                                GetMinReconnectPeriod(), (sender, evt) =>
                                                {
                                                    if (ReferenceEquals(sender, _reconnectHandler))
                                                    {
                                                        TriggerConnectionEvent(ConnectionEvent.ReconnectComplete,
                                                            _reconnectHandler.Session);
                                                    }
                                                });

                                            // Save session while reconnecting.
                                            Debug.Assert(_reconnectingSession == null);
                                            _reconnectingSession = _session;
                                            _session = null;
                                            NotifyConnectivityStateChange(EndpointConnectivityState.Connecting);
                                            currentSessionState = SessionState.Reconnecting;
                                            NotifySubscriptions(_reconnectingSession, true);
                                            (context as TaskCompletionSource)?.TrySetResult();
                                            break;
                                        case SessionState.Connecting:
                                        case SessionState.Disconnected:
                                        case SessionState.Reconnecting:
                                            // Nothing to do in this state
                                            break;
                                    }
                                    break;

                                case ConnectionEvent.ReconnectComplete:
                                    // if session recovered, Session property is not null
                                    var reconnected = _reconnectHandler.Session as OpcUaSession;
                                    switch (currentSessionState)
                                    {
                                        case SessionState.Reconnecting:
                                            //
                                            // Behavior of the reconnect handler is as follows:
                                            // 1) newSession == null
                                            //  => then the old session is still good, we missed keep alive.
                                            // 2) newSession != null but equal to previous session
                                            //  => new channel was opened but the existing session was reactivated
                                            // 3) newSession != previous Session
                                            //  => everything reconnected and new session was activated.
                                            //
                                            reconnected ??= _reconnectingSession;

                                            Debug.Assert(reconnected != null, "reconnected should never be null");
                                            Debug.Assert(reconnected.Connected, "reconnected should always be connected");

                                            // Handles all 3 cases above.
                                            var isNew = await UpdateSessionAsync(reconnected).ConfigureAwait(false);

                                            Debug.Assert(_session != null);
                                            Debug.Assert(_reconnectingSession == null);
                                            if (!isNew)
                                            {
                                                // Case 1) and 2)
                                                _logger.LogInformation("{Client}: Client RECOVERED!", this);
                                            }
                                            else
                                            {
                                                // Case 3)
                                                _logger.LogInformation("{Client}: Client RECONNECTED!", this);
                                                ReconnectCount++;
                                            }

                                            // If not already ready, signal we are ready again and ...
                                            NotifyConnectivityStateChange(EndpointConnectivityState.Ready);
                                            // ... allow access to the client again
                                            Debug.Assert(_disconnectLock != null);
                                            _disconnectLock.Dispose();
                                            _disconnectLock = null;

                                            await SyncAsync(isNew, ct).ConfigureAwait(false);

                                            _reconnectRequired = 0;
                                            reconnectPeriod = GetMinReconnectPeriod();
                                            currentSessionState = SessionState.Connected;
                                            NotifySubscriptions(_session, false);
                                            break;

                                        case SessionState.Connected:
                                            Debug.Fail("Should not signal reconnected when already connected.");
                                            break;
                                        case SessionState.Connecting:
                                        case SessionState.Disconnected:
                                            Debug.Assert(_reconnectingSession == null);
                                            reconnected?.Dispose();
                                            break;
                                    }
                                    break;
                                case ConnectionEvent.Disconnect:
                                    if (currentSessionState != SessionState.Disconnected)
                                    {
                                        await HandleDisconnectEvent(ct).ConfigureAwait(false);
                                        currentSessionState = SessionState.Disconnected;
                                    }
                                    break;
                            }

                            _logger.LogDebug("{Client}: Event {Event} in State {State} processed.", trigger,
                                this, currentSessionState);
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "{Client}: Connection manager exited unexpectedly...", this);
                    }
                    finally
                    {
                        _reconnectHandler.CancelReconnect();
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Client}: Exception in management loop.", this);
                throw;
            }
            finally
            {
                if (currentSessionState != SessionState.Disconnected)
                {
                    _logger.LogInformation(
                        "{Client}: Disconnect because client is disposed.", this);
                    await HandleDisconnectEvent(default).ConfigureAwait(false);
                    currentSessionState = SessionState.Disconnected;
                }
                _logger.LogInformation("{Client}: Exiting client management loop.", this);
            }

            async ValueTask HandleDisconnectEvent(CancellationToken cancellationToken)
            {
                // If currently reconnecting, dispose the reconnect handler and stop timer
                _reconnectHandler.CancelReconnect();
                reconnectTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

                // if not already disconnected, aquire writer lock
                _disconnectLock ??= await _lock.WriterLockAsync(cancellationToken);
                reconnectPeriod = 0;

                NotifyConnectivityStateChange(EndpointConnectivityState.Disconnected);
                NotifySubscriptions(_session, true);

                await CloseSessionAsync().ConfigureAwait(false);
                Debug.Assert(_session == null);
            }

            void NotifySubscriptions(OpcUaSession? session, bool disconnected)
            {
                if (session == null)
                {
                    return;
                }
                lock (_subscriptions)
                {
                    foreach (var h in _subscriptions.Values)
                    {
                        h.NotifySessionConnectionState(disconnected);
                    }
                }
            }

            int GetMinReconnectPeriod()
            {
                var reconnectPeriod = MinReconnectDelay ?? TimeSpan.Zero;
                if (reconnectPeriod == TimeSpan.Zero)
                {
                    reconnectPeriod = TimeSpan.FromSeconds(1);
                }
                if (reconnectPeriod > _maxReconnectPeriod)
                {
                    reconnectPeriod = _maxReconnectPeriod;
                }
                return (int)reconnectPeriod.TotalMilliseconds;
            }
        }

        private const int kMinPublishRequestCount = 2;
        private const int kMaxPublishRequestCount = 10;

        /// <summary>
        /// Ensure min publish requests are configured correctly
        /// </summary>
        private void UpdatePublishRequestCounts()
        {
            var session = _session;
            if (session == null)
            {
                return;
            }

            var minPublishRequests = MinPublishRequests ?? 0;
            if (minPublishRequests <= 0)
            {
                minPublishRequests = kMinPublishRequestCount;
            }

            var maxPublishRequests = MaxPublishRequests ?? kMaxPublishRequestCount;
            if (maxPublishRequests <= 0 || maxPublishRequests > ushort.MaxValue)
            {
                maxPublishRequests = ushort.MaxValue;
            }

            var createdSubscriptions = SubscriptionCount;
            if (PublishRequestsPerSubscriptionPercent.HasValue)
            {
                var percentage = PublishRequestsPerSubscriptionPercent ?? 100;
                var minPublishOverride = percentage == 100 || percentage <= 0 ?
                    createdSubscriptions :
                    (int)Math.Ceiling(createdSubscriptions * (percentage / 100.0));
                if (minPublishRequests < minPublishOverride)
                {
                    minPublishRequests = minPublishOverride;
                }
            }

            Debug.Assert(minPublishRequests > 0);
            Debug.Assert(maxPublishRequests > 0);

            if (minPublishRequests > maxPublishRequests)
            {
                // Dont allow min to be higher than max
                minPublishRequests = maxPublishRequests;
            }

            //
            // The stack will choose a value based on the subscription
            // count that is between min and max.
            //
            session.Subscriptions.MinPublishWorkerCount = minPublishRequests;
            session.Subscriptions.MaxPublishWorkerCount = maxPublishRequests;

            if (createdSubscriptions > 0 && minPublishRequests > PublishWorkerCount)
            {
                session.Subscriptions.Update();
            }
        }

        /// <summary>
        /// Connect client
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async ValueTask<bool> TryConnectAsync(CancellationToken ct)
        {
            var timeout = CreateSessionTimeout ?? TimeSpan.FromSeconds(10);

            NotifyConnectivityStateChange(EndpointConnectivityState.Connecting);
            Debug.Assert(_connection.Endpoint != null);

            _logger.LogInformation("{Client}: Connecting to {EndpointUrl}...",
                this, _connection.Endpoint.Url);
            var attempt = 0;
            foreach (var nextUrl in _connection.GetEndpointUrls())
            {
                var endpointUrl = nextUrl;

                // Ensure any previous session is disposed here.
                await CloseSessionAsync().ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();
                try
                {
                    ITransportWaitingConnection? connection = null;
                    if (_reverseConnectManager != null)
                    {
                        connection = await _reverseConnectManager.WaitForConnectionAsync(
                            endpointUrl, null, ct).ConfigureAwait(false);
                    }

                    //
                    // Get the endpoint by connecting to server's discovery endpoint.
                    // Try to find the first endpoint with security.
                    //
                    var securityMode = _connection.Endpoint.SecurityMode ?? SecurityMode.NotNone;
                    var securityProfile = _connection.Endpoint.SecurityPolicy;
                    var endpointDescription = await SelectEndpointAsync(_configuration,
                        endpointUrl, connection, securityMode, securityProfile, _logger,
                        this, ct: ct).ConfigureAwait(false);
                    if (endpointDescription == null)
                    {
                        _logger.LogWarning(
                            "{Client}: No endpoint found that matches connection of session {Name}.",
                            this, _sessionName);
                        continue;
                    }

                    endpointUrl = Utils.ParseUri(endpointDescription.EndpointUrl);
                    var endpointConfiguration = EndpointConfiguration.Create(
                        _configuration);
                    endpointConfiguration.OperationTimeout =
                        (int)timeout.TotalMilliseconds;
                    var endpoint = new ConfiguredEndpoint(null, endpointDescription,
                        endpointConfiguration);

                    var credential = _connection.User;
                    if (securityMode == SecurityMode.Best &&
                        endpointDescription.SecurityMode == MessageSecurityMode.None)
                    {
                        _logger.LogWarning("{Client}: Although the use of best security was " +
                            "configured, there was no security-enabled endpoint available at " +
                            "url {EndpointUrl}. An endpoint with no security will be used " +
                            "for session {Name} but no credentials will be sent over it.",
                            this, endpointUrl, _sessionName);

                        credential = null;
                    }

                    var userIdentity = await credential.ToUserIdentityAsync(
                        _configuration).ConfigureAwait(false);

                    var identityPolicy = endpoint.Description.FindUserTokenPolicy(
                        userIdentity.TokenType, userIdentity.IssuedTokenType);
                    if (identityPolicy == null)
                    {
                        _logger.LogWarning(
                            "{Client}: No UserTokenPolicy for {TokenType}/{IssuedTokenType} " +
                            "found on endpoint {EndpointUrl} (session: {Name}).",
                            this, userIdentity.TokenType, userIdentity.IssuedTokenType,
                            endpointUrl, _sessionName);
                        continue;
                    }
                    _logger.LogInformation(
                        "{Client}: #{Attempt} - Creating session {Name} with endpoint {EndpointUrl}...",
                        ++attempt, this, _sessionName, endpointUrl);

                    var preferredLocales = _connection.Locales?.ToList() ?? new List<string>();
                    if (preferredLocales.Count == 0)
                    {
                        // Create the session with english as default
                        preferredLocales.Add("en-US");
                        if (CultureInfo.CurrentCulture.Name != preferredLocales[0])
                        {
                            // and current language locale as backup
                            preferredLocales.Add(CultureInfo.CurrentCulture.Name);
                        }
                    }
#pragma warning disable CA2000 // Dispose objects before losing scope
                    var session = new OpcUaSession(this, _serializer, _configuration,
                        endpoint, new SessionOptions
                        {
                            SessionName = _sessionName,
                            Connection = connection,
                            SessionTimeout = SessionTimeout,
                            Identity = userIdentity,
                            CheckDomain = false,
                            PreferredLocales = preferredLocales
                        }, _observability, _reverseConnectManager);
#pragma warning restore CA2000 // Dispose objects before losing scope
                    try
                    {
                        await session.OpenAsync(null, ct).ConfigureAwait(false);

                        // Assign the createdSubscriptions session
                        var isNew = await UpdateSessionAsync(session).ConfigureAwait(false);
                        Debug.Assert(isNew);
                        _logger.LogInformation(
                            "{Client}: New Session {Name} created with endpoint {EndpointUrl} ({Original}).",
                            this, _sessionName, endpointUrl, _connection.Endpoint.Url);

                        _logger.LogInformation("{Client} Client CONNECTED to {EndpointUrl}!",
                            this, endpointUrl);
                        return true;
                    }
                    catch
                    {
                        session.Dispose();
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    NotifyConnectivityStateChange(ToConnectivityState(ex));
                    ReconnectCount++;
                    _logger.LogInformation(
                        "#{Attempt} - {Client}: Failed to connect to {EndpointUrl}: {Message}...",
                        ++attempt, this, endpointUrl, ex.Message);
                }
            }
            return false;
        }

        /// <summary>
        /// Handles a keep alive event from a session and triggers a reconnect
        /// if necessary.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="serviceResult"></param>
        internal void Session_KeepAlive(OpcUaSession session, ServiceResult serviceResult)
        {
            if (_disposed)
            {
                return;
            }
            try
            {
                // check for events from discarded sessions.
                if (!ReferenceEquals(session, _session))
                {
                    return;
                }
                // start reconnect sequence on communication error.
                Interlocked.Increment(ref _keepAliveCounter);
                if (ServiceResult.IsBad(serviceResult))
                {
                    _keepAliveCounter = 0;
                    TriggerReconnect(serviceResult, "Keep alive");
                }
                else if (SubscriptionCount > 0 && GoodPublishRequestCount == 0)
                {
                    UpdatePublishRequestCounts();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Client}: Error in OnKeepAlive.", this);
            }
        }

        /// <summary>
        /// Trigger reconnect
        /// </summary>
        /// <param name="sr"></param>
        /// <param name="action"></param>
        private void TriggerReconnect(ServiceResult sr, string action)
        {
            if (Interlocked.Increment(ref _reconnectRequired) == 1)
            {
                _logger.LogError(
                    "{Client}: Error {Error} during {Action} - triggering reconnect...",
                    this, sr, action);

                // Ensure we reconnect
                TriggerConnectionEvent(ConnectionEvent.StartReconnect, sr);
            }
        }

        /// <summary>
        /// Trigger connection event
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="context"></param>
        private void TriggerConnectionEvent(ConnectionEvent evt, object? context = null)
        {
            _channel.Writer.TryWrite((evt, context));
        }

        /// <summary>
        /// Update session state
        /// </summary>
        /// <param name="session"></param>
        private async ValueTask<bool> UpdateSessionAsync(SessionBase session)
        {
            _publishTimeoutCounter = 0;
            Debug.Assert(session is OpcUaSession);

            if (_session == null)
            {
                _session = _reconnectingSession;
                _reconnectingSession = null;
            }

            Debug.Assert(_reconnectingSession == null);
            var isNewSession = false;
            if (!ReferenceEquals(_session, session))
            {
                await CloseSessionAsync().ConfigureAwait(false);
                _session = (OpcUaSession)session;
                isNewSession = true;

                kSessions.Add(1, _metrics.TagList);
                ConnectCount++;
            }

            UpdatePublishRequestCounts();
            NotifyConnectivityStateChange(EndpointConnectivityState.Ready);
            UpdateNamespaceTableAndSessionDiagnostics(_session);
            return isNewSession;

            void UpdateNamespaceTableAndSessionDiagnostics(OpcUaSession session)
            {
                lock (_channelLock)
                {
                    UpdateConnectionDiagnosticFromSession(session);
                }
            }
        }

        /// <summary>
        /// Update diagnostic if the channel has changed
        /// </summary>
        private void OnUpdateConnectionDiagnostics()
        {
            if (_session != null)
            {
                lock (_channelLock)
                {
                    UpdateConnectionDiagnosticFromSession(_session);
                }
            }
        }

        /// <summary>
        /// Update session diagnostics
        /// </summary>
        /// <param name="session"></param>
        private void UpdateConnectionDiagnosticFromSession(OpcUaSession session)
        {
            // Called under lock

            var channel = session.TransportChannel;
            var token = channel?.CurrentToken;

            var now = _observability.TimeProvider.GetUtcNow();

            var lastDiagnostics = LastDiagnostics;
            var elapsed = now - lastDiagnostics.TimeStamp;

            var channelChanged = false;
            if (token != null)
            {
                //
                // Monitor channel's token lifetime and update diagnostics
                // Check wether the token or channel changed. If so set a
                // timer to monitor the new token lifetime, if not then
                // try again after the remaining lifetime or every second
                // until it changed unless the token is then later gone.
                //
                channelChanged = !(lastDiagnostics != null &&
                    lastDiagnostics.ChannelId == token.ChannelId &&
                    lastDiagnostics.TokenId == token.TokenId &&
                    lastDiagnostics.CreatedAt == token.CreatedAt);

                var lifetime = TimeSpan.FromMilliseconds(Math.Min(token.Lifetime,
                    _configuration.TransportQuotas.SecurityTokenLifetime));
                if (channelChanged)
                {
                    _channelMonitor.Change(lifetime, Timeout.InfiniteTimeSpan);
                    _logger.LogInformation(
                        "Channel {Channel} got new token {TokenId} ({Created}).",
                        token.ChannelId, token.TokenId, token.CreatedAt);
                }
                else
                {
                    //
                    // Token has not yet been updated, let's retry later
                    // It is also assumed that the port/ip are still the same
                    //
                    if (lifetime > elapsed)
                    {
                        _channelMonitor.Change(lifetime - elapsed,
                            Timeout.InfiniteTimeSpan);
                    }
                    else
                    {
                        _channelMonitor.Change(TimeSpan.FromSeconds(1),
                            Timeout.InfiniteTimeSpan);
                    }
                }
            }

            var sessionId = session.SessionId?.AsString(session.MessageContext,
                NamespaceFormat.Index);

            // Get effective ip address and port
            var socket = (channel as UaSCUaBinaryTransportChannel)?.Socket;
            var remoteIpAddress = socket?.RemoteEndpoint?.GetIPAddress()?.ToString();
            var remotePort = socket?.RemoteEndpoint?.GetPort();
            var localIpAddress = socket?.LocalEndpoint?.GetIPAddress()?.ToString();
            var localPort = socket?.LocalEndpoint?.GetPort();

            if (LastDiagnostics.SessionCreated == session.CreatedAt &&
                LastDiagnostics.SessionId == sessionId &&
                LastDiagnostics.RemoteIpAddress == remoteIpAddress &&
                LastDiagnostics.RemotePort == remotePort &&
                LastDiagnostics.LocalIpAddress == localIpAddress &&
                LastDiagnostics.LocalPort == localPort &&
                !channelChanged)
            {
                return;
            }

            LastDiagnostics = new ChannelDiagnosticModel
            {
                Connection = _connection,
                TimeStamp = now,
                SessionCreated = session.CreatedAt,
                SessionId = sessionId,
                RemoteIpAddress = remoteIpAddress,
                RemotePort = remotePort == -1 ? null : remotePort,
                LocalIpAddress = localIpAddress,
                LocalPort = localPort == -1 ? null : localPort,
                ChannelId = token?.ChannelId,
                TokenId = token?.TokenId,
                CreatedAt = token?.CreatedAt,
                Lifetime = token == null ? null :
                    TimeSpan.FromMilliseconds(token.Lifetime),
                Client = ToChannelKey(token?.ClientInitializationVector,
                    token?.ClientEncryptingKey, token?.ClientSigningKey),
                Server = ToChannelKey(token?.ServerInitializationVector,
                    token?.ServerEncryptingKey, token?.ServerSigningKey)
            };
            _diagnosticsCb(LastDiagnostics);

            _logger.LogInformation("Channel diagnostics for session {SessionId} updated.",
                sessionId);

            static ChannelKeyModel? ToChannelKey(byte[]? iv, byte[]? key, byte[]? sk)
            {
                if (iv == null || key == null || sk == null ||
                    iv.Length == 0 || key.Length == 0 || sk.Length == 0)
                {
                    return null;
                }
                return new ChannelKeyModel
                {
                    Iv = iv,
                    Key = key,
                    SigLen = sk.Length
                };
            }
        }

        /// <summary>
        /// Unset and dispose existing session
        /// </summary>
        /// <param name="shutdown"></param>
        /// <returns></returns>
        private async ValueTask CloseSessionAsync(bool shutdown = false)
        {
            if (_reconnectingSession != null)
            {
                await DisposeAsync(_reconnectingSession).ConfigureAwait(false);
                _reconnectingSession = null;
            }

            if (_session != null)
            {
                await DisposeAsync(_session).ConfigureAwait(false);
                _session = null;
            }

            _publishTimeoutCounter = 0;

            async ValueTask DisposeAsync(OpcUaSession session)
            {
                try
                {
                    if (shutdown)
                    {
                        // When shutting down, delete all subscriptions
                        session.DeleteSubscriptionsOnClose = true;
                    }
                    await session.CloseAsync(CancellationToken.None).ConfigureAwait(false);

                    _logger.LogDebug("{Client}: Successfully closed session {Session}.",
                        this, session);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{Client}: Failed to close session {Session}.",
                        this, session);
                }
                finally
                {
                    session.Dispose();
                    kSessions.Add(-1, _metrics.TagList);
                }
                Debug.Assert(session.Subscriptions.Count == 0);
            }
        }

        /// <summary>
        /// Notify about new connectivity state using any status callback registered.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        private void NotifyConnectivityStateChange(EndpointConnectivityState state)
        {
            var previous = State;
            if (previous == state)
            {
                return;
            }
            if (previous != EndpointConnectivityState.Connecting &&
                previous != EndpointConnectivityState.Ready &&
                state == EndpointConnectivityState.Error)
            {
                // Do not change state to generic error once we have
                // a specific error state already set...
                _logger.LogDebug(
                    "{Client}: Error, connection to {Endpoint} - leaving state at {Previous}.",
                    this, _connection.Endpoint!.Url, previous);
                return;
            }
            State = state;

            if (state == EndpointConnectivityState.Ready)
            {
                lock (_browsers)
                {
                    foreach (var browser in _browsers.Values)
                    {
                        browser.OnConnected();
                    }
                }
            }

            _logger.LogInformation(
                "{Client}: Session {Name} with {Endpoint} changed from {Previous} to {State}",
                this, _sessionName, _connection.Endpoint!.Url, previous, state);
            try
            {
                _notifier?.Invoke(this, new EndpointConnectivityStateEventArgs(state));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Client}: Exception during state callback", this);
            }
        }

        /// <summary>
        /// Select the endpoint to use
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="discoveryUrl"></param>
        /// <param name="connection"></param>
        /// <param name="securityMode"></param>
        /// <param name="securityPolicy"></param>
        /// <param name="logger"></param>
        /// <param name="context"></param>
        /// <param name="endpointUrl"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal static async Task<EndpointDescription?> SelectEndpointAsync(
            ApplicationConfiguration configuration, Uri? discoveryUrl,
            ITransportWaitingConnection? connection, SecurityMode securityMode,
            string? securityPolicy, ILogger logger, object? context,
            string? endpointUrl = null, CancellationToken ct = default)
        {
            var endpointConfiguration = EndpointConfiguration.Create();
            endpointConfiguration.OperationTimeout =
                (int)TimeSpan.FromSeconds(15).TotalMilliseconds;

            // needs to add the /discovery onto http urls
            if (connection == null)
            {
                if (discoveryUrl == null)
                {
                    return null;
                }
                if (discoveryUrl.Scheme == Utils.UriSchemeHttp &&
                    !discoveryUrl.AbsolutePath.EndsWith("/discovery",
                        StringComparison.OrdinalIgnoreCase))
                {
                    discoveryUrl = new UriBuilder(discoveryUrl)
                    {
                        Path = discoveryUrl.AbsolutePath.TrimEnd('/') + "/discovery"
                    }.Uri;
                }
            }

            using var client = connection != null ?
                DiscoveryClient.Create(configuration, connection, endpointConfiguration) :
                DiscoveryClient.Create(configuration, discoveryUrl, endpointConfiguration);
            var uri = new Uri(endpointUrl ?? client.Endpoint.EndpointUrl);
            var endpoints = await client.GetEndpointsAsync(null, ct).ConfigureAwait(false);
            discoveryUrl ??= uri;

            logger.LogInformation("{Client}: Discovery endpoint {DiscoveryUrl} returned endpoints. " +
                "Selecting endpoint {EndpointUri} with SecurityMode " +
                "{SecurityMode} and {SecurityPolicy} SecurityPolicyUri from:\n{Endpoints}",
                context, discoveryUrl, uri, securityMode, securityPolicy ?? "any", endpoints.Select(
                    ep => "      " + ToString(ep)).Aggregate((a, b) => $"{a}\n{b}"));

            var filtered = endpoints
                .Where(ep =>
                    SecurityPolicies.GetDisplayName(ep.SecurityPolicyUri) != null &&
                    ep.SecurityMode.IsSame(securityMode) &&
                    (securityPolicy == null ||
                     string.Equals(ep.SecurityPolicyUri,
                        securityPolicy,
                        StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(ep.SecurityPolicyUri,
                        "http://opcfoundation.org/UA/SecurityPolicy#" + securityPolicy,
                        StringComparison.OrdinalIgnoreCase)))
                //
                // The security level is a relative measure assigned by the server
                // to the endpoints that it returns. Clients should always pick the
                // highest level unless they have a reason not too. Some servers
                // however, mess this up a bit. So group SecurityLevel also by
                // security mode and then pick the highest in that group.
                //
                .OrderByDescending(ep => ((int)ep.SecurityMode << 8) | ep.SecurityLevel)
                .ToList();

            //
            // Try to find endpoint that matches scheme and endpoint url path
            // but fall back to match just the scheme. We need to match only
            // scheme to support the reverse connect (indicated by connection
            // being not null here).
            //
            var selected = filtered.Find(ep => Match(ep, uri, true, true))
                        ?? filtered.Find(ep => Match(ep, uri, true, false));
            if (connection != null)
            {
                //
                // Only allow same uri scheme (which must also be opc.tcp)
                // for when reverse connection is used.
                //
                if (selected != null)
                {
                    logger.LogInformation(
                        "{Client}: Endpoint {Endpoint} selected via reverse connect!",
                        context, ToString(selected));
                }
                return selected;
            }

            if (selected == null)
            {
                //
                // Fall back to first supported endpoint matching absolute path
                // then fall back to first endpoint (backwards compatibilty)
                //
                selected = filtered.Find(ep => Match(ep, uri, false, true))
                        ?? filtered.Find(ep => Match(ep, uri, false, false));

                if (selected == null)
                {
                    return null;
                }
            }

            //
            // Adjust the host name and port to the host name and port
            // that was use to successfully connect the discovery client
            //
            var selectedUrl = Utils.ParseUri(selected.EndpointUrl);
            if (selectedUrl != null && discoveryUrl != null &&
                selectedUrl.Scheme == discoveryUrl.Scheme)
            {
                selected.EndpointUrl = new UriBuilder(selectedUrl)
                {
                    Host = discoveryUrl.DnsSafeHost,
                    Port = discoveryUrl.Port
                }.ToString();
            }

            logger.LogInformation("{Client}: Endpoint {Endpoint} selected!", context,
                ToString(selected));
            return selected;

            static string ToString(EndpointDescription ep) =>
$"#{ep.SecurityLevel:000}: {ep.EndpointUrl}|{ep.SecurityMode} [{ep.SecurityPolicyUri}]";
            // Match endpoint returned against desired endpoint url
            static bool Match(EndpointDescription endpointDescription,
                Uri endpointUrl, bool includeScheme, bool includePath)
            {
                var url = Utils.ParseUri(endpointDescription.EndpointUrl);
                return url != null &&
                    (!includeScheme || string.Equals(url.Scheme,
                        endpointUrl.Scheme, StringComparison.OrdinalIgnoreCase)) &&
                    (!includePath || string.Equals(url.AbsolutePath,
                        endpointUrl.AbsolutePath, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Convert exception to connectivity state
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="reconnecting"></param>
        /// <returns></returns>
        public EndpointConnectivityState ToConnectivityState(Exception ex, bool reconnecting = true)
        {
            EndpointConnectivityState state;
            switch (ex)
            {
                case ServiceResultException sre:
                    switch (sre.StatusCode)
                    {
                        case StatusCodes.BadNoContinuationPoints:
                        case StatusCodes.BadLicenseLimitsExceeded:
                        case StatusCodes.BadTcpServerTooBusy:
                        case StatusCodes.BadTooManySessions:
                        case StatusCodes.BadTooManyOperations:
                            state = EndpointConnectivityState.Busy;
                            break;
                        case StatusCodes.BadCertificateRevocationUnknown:
                        case StatusCodes.BadCertificateIssuerRevocationUnknown:
                        case StatusCodes.BadCertificateRevoked:
                        case StatusCodes.BadCertificateIssuerRevoked:
                        case StatusCodes.BadCertificateChainIncomplete:
                        case StatusCodes.BadCertificateIssuerUseNotAllowed:
                        case StatusCodes.BadCertificateUseNotAllowed:
                        case StatusCodes.BadCertificateUriInvalid:
                        case StatusCodes.BadCertificateTimeInvalid:
                        case StatusCodes.BadCertificateIssuerTimeInvalid:
                        case StatusCodes.BadCertificateInvalid:
                        case StatusCodes.BadCertificateHostNameInvalid:
                        case StatusCodes.BadNoValidCertificates:
                            state = EndpointConnectivityState.CertificateInvalid;
                            break;
                        case StatusCodes.BadCertificateUntrusted:
                        case StatusCodes.BadSecurityChecksFailed:
                            state = EndpointConnectivityState.NoTrust;
                            break;
                        case StatusCodes.BadSecureChannelClosed:
                            state = reconnecting ? EndpointConnectivityState.NoTrust :
                                EndpointConnectivityState.Error;
                            break;
                        case StatusCodes.BadRequestTimeout:
                        case StatusCodes.BadNotConnected:
                            state = EndpointConnectivityState.NotReachable;
                            break;
                        case StatusCodes.BadUserAccessDenied:
                        case StatusCodes.BadUserSignatureInvalid:
                            state = EndpointConnectivityState.Unauthorized;
                            break;
                        default:
                            state = EndpointConnectivityState.Error;
                            break;
                    }
                    _logger.LogDebug("{Client}: {Result} => {State}", this, sre.Result, state);
                    break;
                default:
                    state = EndpointConnectivityState.Error;
                    _logger.LogDebug("{Client}: {Message} => {State}", this, ex.Message, state);
                    break;
            }
            return state;
        }

        /// <summary>
        /// Get the real timeout for the service call
        /// </summary>
        /// <param name="serviceCallTimeout"></param>
        /// <returns></returns>
        private TimeSpan GetServiceCallTimeout(int? serviceCallTimeout)
        {
            if (serviceCallTimeout > 0)
            {
                return TimeSpan.FromMilliseconds(serviceCallTimeout.Value);
            }
            if (ServiceCallTimeout.HasValue)
            {
                return ServiceCallTimeout.Value;
            }
            if (OperationTimeout.HasValue && OperationTimeout > kDefaultServiceCallTimeout)
            {
                return OperationTimeout.Value;
            }
            return kDefaultServiceCallTimeout;
        }

        /// <summary>
        /// Get the real timeout for the connectivity of session
        /// </summary>
        /// <param name="connectTimeout"></param>
        /// <param name="serviceCallTimeout"></param>
        /// <returns></returns>
        private TimeSpan GetConnectCallTimeout(int? connectTimeout, int? serviceCallTimeout)
        {
            if (connectTimeout > 0)
            {
                return TimeSpan.FromMilliseconds(connectTimeout.Value);
            }
            if (ConnectTimeout.HasValue)
            {
                return ConnectTimeout.Value;
            }
            if (serviceCallTimeout > 0)
            {
                return TimeSpan.FromMilliseconds(serviceCallTimeout.Value);
            }
            return ServiceCallTimeout ?? kDefaultConnectTimeout;
        }

        /// <summary>
        /// Dump diagnostics
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task DumpDiagnosticsPeriodicallyAsync(CancellationToken ct)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await timer.WaitForNextTickAsync(ct).ConfigureAwait(false);
                    if (_session != null)
                    {
                        var diagnostics = await _session.GetServerDiagnosticAsync(
                            ct).ConfigureAwait(false);
                        var str = JsonSerializer.Serialize(diagnostics, kIndented);
                        Console.WriteLine(str);
                    }
                }
            }
            catch (OperationCanceledException) { }
        }
        private static readonly JsonSerializerOptions kIndented = new()
        {
            WriteIndented = true
        };

        private enum ConnectionEvent
        {
            Connect,
            ConnectRetry,
            Disconnect,
            StartReconnect,
            ReconnectComplete,
            Reset,
            SubscriptionSyncOne,
            SubscriptionSyncAll
        }

        private enum SessionState
        {
            Disconnected,
            Connecting,
            Connected,
            Reconnecting
        }

        /// <summary>
        /// Disconnected state
        /// </summary>
        private sealed class DisconnectState : IOpcUaClientDiagnostics
        {
            /// <inheritdoc/>
            public int BadPublishRequestCount
                => 0;
            /// <inheritdoc/>
            public int GoodPublishRequestCount
                => 0;
            /// <inheritdoc/>
            public int PublishWorkerCount
                => 0;
            /// <inheritdoc/>
            public int SubscriptionCount
                => 0;
            /// <inheritdoc/>
            public EndpointConnectivityState State
                => EndpointConnectivityState.Disconnected;
            /// <inheritdoc/>
            public int ReconnectCount
                => 0;
            /// <inheritdoc/>
            public int ConnectCount
                => 0;
            /// <inheritdoc/>
            public int MinPublishRequestCount
                => 0;
        }

        /// <summary>
        /// Create observable metrics
        /// </summary>
        private void InitializeMetrics()
        {
            _meter.CreateObservableGauge("iiot_edge_publisher_client_connectivity_state",
                () => new Measurement<int>((int)State, _metrics.TagList),
                description: "Client connectivity state.");
            _meter.CreateObservableGauge("iiot_edge_publisher_client_keep_alive_counter",
                () => new Measurement<int>(_keepAliveCounter, _metrics.TagList),
                description: "Number of successful keep alives since last keep alive error.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_client_namespace_change_count",
                () => new Measurement<int>(_session?.NamespaceTableChanges ?? 0, _metrics.TagList),
                description: "Number of namespace table changes detected by the client.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_subscriptions",
                () => new Measurement<int>(SubscriptionCount, _metrics.TagList),
                description: "Number of client managed subscriptions.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_client_sampler_count",
                () => new Measurement<int>(_samplers.Count, _metrics.TagList),
                description: "Number of active client samplers.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_client_browser_count",
                () => new Measurement<int>(_browsers.Count, _metrics.TagList),
                description: "Number of active browsers.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_client_connectivity_retry_count",
                () => new Measurement<int>(ReconnectCount, _metrics.TagList),
                description: "Number of connectivity retries on this connection.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_client_connectivity_count",
                () => new Measurement<int>(ConnectCount, _metrics.TagList),
                description: "Number of sessions established as a total for the client.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_client_ref_count",
                () => new Measurement<int>(_refCount, _metrics.TagList),
                description: "Number of references to this client.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_client_good_publish_requests_count",
                () => new Measurement<int>(GoodPublishRequestCount, _metrics.TagList),
                description: "Number of good publish requests.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_client_bad_publish_requests_count",
                () => new Measurement<int>(BadPublishRequestCount, _metrics.TagList),
                description: "Number of bad publish requests.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_client_min_publish_requests_count",
                () => new Measurement<int>(MinPublishRequestCount, _metrics.TagList),
                description: "Number of min publish requests that should be queued.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_client_outstanding_requests_count",
                () => new Measurement<int>(PublishWorkerCount, _metrics.TagList),
                description: "Number of outstanding requests.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_client_publish_timeout_count",
                () => new Measurement<int>(_publishTimeoutCounter, _metrics.TagList),
                description: "Number of timed out requests.");
        }

        private static readonly UpDownCounter<int> kSessions = Diagnostics.Meter.CreateUpDownCounter<int>(
            "iiot_edge_publisher_session_count", description: "Number of active sessions.");

        private OpcUaSession? _reconnectingSession;
        private int _reconnectRequired;
#pragma warning disable CA2213 // Disposable fields should be disposed
        private OpcUaSession? _session;
        private IDisposable? _disconnectLock;
#pragma warning restore CA2213 // Disposable fields should be disposed
        private bool _disposed;
        private int _refCount;
        private int _publishTimeoutCounter;
        private int _keepAliveCounter;
        private readonly ReverseConnectManager? _reverseConnectManager;
        private readonly AsyncReaderWriterLock _lock = new();
        private readonly ApplicationConfiguration _configuration;
        private readonly IJsonSerializer _serializer;
        private readonly Meter _meter;
        private readonly string _sessionName;
        private readonly IOptions<OpcUaClientOptions> _options;
        private readonly ConnectionModel _connection;
        private readonly ILogger _logger;
        private readonly IObservability _observability;
        private readonly IMetricsContext _metrics;
        private readonly object _channelLock = new();
#pragma warning disable CA2213 // Disposable fields should be disposed
        private readonly ITimer _channelMonitor;
        private readonly Task? _diagnosticsDumper;
        private readonly SessionReconnectHandler _reconnectHandler;
        private readonly CancellationTokenSource _cts;
#pragma warning restore CA2213 // Disposable fields should be disposed
        private readonly Task _sessionManager;
        private readonly TimeSpan _maxReconnectPeriod;
        private readonly Channel<(ConnectionEvent, object?)> _channel;
        private readonly Action<ChannelDiagnosticModel> _diagnosticsCb;
        private readonly EventHandler<EndpointConnectivityStateEventArgs>? _notifier;
        private readonly Dictionary<(OpcUaSubscription, TimeSpan, TimeSpan), Sampler> _samplers = new();
        private readonly Dictionary<(OpcUaSubscription, TimeSpan), Browser> _browsers = new();
        private readonly Dictionary<string, CancellationTokenSource> _tokens;
        private static readonly TimeSpan kDefaultServiceCallTimeout = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan kDefaultConnectTimeout = TimeSpan.FromMinutes(1);
    }
}
