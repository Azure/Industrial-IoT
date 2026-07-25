// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using Azure.IIoT.OpcUa.Core.Serialization;
    using Azure.IIoT.OpcUa.Core.Utils;
    using Azure.IIoT.OpcUa.Exceptions;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Extensions;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Client.Subscriptions;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.ExceptionServices;
    using System.Threading;
    using System.Threading.Tasks;
    using OpcUaClientOptions = Azure.IIoT.OpcUa.Publisher.Stack.OpcUaClientOptions;
    using PublisherSubscription = Azure.IIoT.OpcUa.Publisher.Stack.ISubscription;

    /// <summary>
    /// Internal runtime surface selected by the client-manager composition root.
    /// </summary>
    /// <remarks>
    /// Production DI selects the managed strategy. Direct construction can still omit
    /// the strategy to exercise the classic rollback path until classic removal.
    /// </remarks>
    internal interface IOpcUaClientRuntime : IDisposable
    {
        ChannelDiagnosticModel LastDiagnostics { get; }
        Task<ISessionHandle> AcquireAsync(int? connectTimeout,
            int? serviceCallTimeout, CancellationToken ct);
        Task<T> RunAsync<T>(Func<ServiceCallContext, Task<T>> service,
            int? connectTimeout, int? serviceCallTimeout, CancellationToken ct);
        IAsyncEnumerable<T> RunAsync<T>(AsyncEnumerableBase<T> operation,
            int? connectTimeout, int? serviceCallTimeout, CancellationToken ct);
        ValueTask<PublisherSubscription> RegisterAsync(SubscriptionModel subscription,
            ISubscriber subscriber, CancellationToken ct);
        Task ResetAsync(CancellationToken ct);
        Task<SessionDiagnosticsModel?> GetSessionDiagnosticsAsync(CancellationToken ct);
        ValueTask CloseAsync(bool shutdown = false, bool fromManagementLoop = false);
        bool TryAddRef();
        void AddRef(string? token = null, TimeSpan? expiresAfter = null);
    }

    /// <summary>
    /// Factory for the runtime selected for one connection.
    /// </summary>
    internal interface IOpcUaClientRuntimeStrategy
    {
        IOpcUaClientRuntime Create(OpcUaClientRuntimeContext context);
        ValueTask DisposeAsync();
    }

    /// <summary>
    /// Inputs shared by the classic and managed client runtimes.
    /// </summary>
    internal sealed record class OpcUaClientRuntimeContext
    {
        public required ApplicationConfiguration Configuration { get; init; }
        public required ConnectionIdentifier Connection { get; init; }
        public required ILoggerFactory LoggerFactory { get; init; }
        public required TimeProvider TimeProvider { get; init; }
        public required IMetricsContext Metrics { get; init; }
        public required Func<Task> OnClose { get; init; }
        public EventHandler<EndpointConnectivityStateEventArgs>? Notifier { get; init; }
        public required ReverseConnectManager ReverseConnectManager { get; init; }
        public required Action<ChannelDiagnosticModel> DiagnosticsCallback { get; init; }
        public required IOptions<OpcUaClientOptions> ClientOptions { get; init; }
        public required IOptions<OpcUaSubscriptionOptions> SubscriptionOptions { get; init; }
        public IOpcUaEndpointSelector EndpointSelector { get; init; }
            = OpcUaEndpointSelector.Instance;
    }

    /// <summary>
    /// Creates the managed-session request for a managed runtime connection.
    /// </summary>
    /// <remarks>
    /// This seam lets production composition and comparison tests translate
    /// connection data without exposing a public runtime switch.
    /// </remarks>
    internal interface IManagedSessionRequestFactory
    {
        Task<ManagedSessionConnectionRequest> CreateAsync(
            ManagedSessionClientContext context, CancellationToken ct);
    }

    /// <summary>
    /// Inputs used by a managed-session request factory.
    /// </summary>
    internal sealed record class ManagedSessionClientContext
    {
        public required ApplicationConfiguration Configuration { get; init; }
        public required ConnectionIdentifier Connection { get; init; }
        public required ILogger Logger { get; init; }
        public required IOptions<OpcUaClientOptions> Options { get; init; }
        public required ReverseConnectManager ReverseConnectManager { get; init; }
        public required TimeProvider TimeProvider { get; init; }
        public int? ConnectTimeout { get; init; }
    }

    /// <summary>
    /// Selects an endpoint and translates Publisher connection data to the managed
    /// session connector request.
    /// </summary>
    internal sealed class DefaultManagedSessionRequestFactory : IManagedSessionRequestFactory
    {
        /// <summary>
        /// Create the default request factory.
        /// </summary>
        public DefaultManagedSessionRequestFactory(
            IOpcUaEndpointSelector? endpointSelector = null,
            Func<ReverseConnectManager, Uri, CancellationToken,
                Task<ITransportWaitingConnection>>? reverseConnectionWaiter = null)
        {
            _endpointSelector = endpointSelector ?? OpcUaEndpointSelector.Instance;
            _reverseConnectionWaiter = reverseConnectionWaiter ??
                ((manager, endpointUrl, ct) =>
                    manager.WaitForConnectionAsync(endpointUrl, null, ct));
        }

        public async Task<ManagedSessionConnectionRequest> CreateAsync(
            ManagedSessionClientContext context, CancellationToken ct)
        {
            var connection = context.Connection.Connection;
            var endpointModel = connection.Endpoint ??
                throw new ArgumentException("Missing endpoint.", nameof(context));
            var securityMode = endpointModel.SecurityMode ?? SecurityMode.NotNone;
            var connectTimeout = GetConnectTimeout(context.ConnectTimeout, context.Options.Value);
            var endpoints = new List<ConfiguredEndpoint>();
            Exception? lastError = null;
            foreach (var endpointUrl in connection.GetEndpointUrls())
            {
                try
                {
                    ITransportWaitingConnection? waitingConnection = null;
                    if (connection.IsReverseConnect())
                    {
                        var reverseConnectManager = context.ReverseConnectManager ??
                            throw new InvalidOperationException(
                                "Reverse connect requires a reverse connect manager.");
                        waitingConnection = await _reverseConnectionWaiter(
                            reverseConnectManager, endpointUrl, ct).ConfigureAwait(false);
                    }
                    var description = await _endpointSelector.SelectAsync(
                        context.Configuration, endpointUrl, waitingConnection, securityMode,
                        endpointModel.SecurityPolicy, context.Logger, context.Connection,
                        ct: ct).ConfigureAwait(false);
                    if (description == null)
                    {
                        continue;
                    }
                    var endpointConfiguration =
                        EndpointConfiguration.Create(context.Configuration);
                    endpointConfiguration.OperationTimeout =
                        ManagedSessionOptionsAdapter.GetEndpointOperationTimeout(
                            context, connectTimeout);
                    endpoints.Add(new ConfiguredEndpoint(null, description,
                        endpointConfiguration));
                    if (connection.IsReverseConnect())
                    {
                        break;
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                }
            }
            if (endpoints.Count == 0)
            {
                throw lastError ?? new ConnectionException(
                    "No matching endpoint was found.");
            }
            var endpoint = endpoints[0];
            var endpointDescription = endpoint.Description;

            var credential = connection.User;
            if (securityMode == SecurityMode.Best &&
                endpointDescription.SecurityMode == MessageSecurityMode.None)
            {
                credential = null;
            }
            var identity = await credential.ToUserIdentityAsync(context.Configuration, ct)
                .ConfigureAwait(false);
            var locales = connection.Locales?.ToList() ?? [];
            if (locales.Count == 0)
            {
                locales.Add("en-US");
                if (!string.Equals(CultureInfo.CurrentCulture.Name, locales[0],
                    StringComparison.Ordinal))
                {
                    locales.Add(CultureInfo.CurrentCulture.Name);
                }
            }
            return new ManagedSessionConnectionRequest
            {
                Connection = context.Connection,
                Endpoint = endpoint,
                AlternativeEndpoints = endpoints.Skip(1).ToArray(),
                Identity = identity,
                PreferredLocales = locales,
                SessionTimeout = context.Options.Value.DefaultSessionTimeoutDuration ??
                    TimeSpan.FromSeconds(30),
                ConnectTimeout = connectTimeout,
                SubscriptionEngineFactory =
                    ManagedSessionOptionsAdapter.CreateSubscriptionEngineFactory(
                        context.TimeProvider),
                ReconnectPolicy =
                    ManagedSessionOptionsAdapter.CreateReconnectPolicy(context.Options.Value),
                TransferSubscriptionsOnRecreate =
                    ManagedSessionOptionsAdapter.TransferSubscriptionsOnRecreate(connection),
                KeepAliveInterval = context.Options.Value.KeepAliveIntervalDuration,
                MinPublishWorkerCount =
                    ManagedSessionOptionsAdapter.GetPublishWorkerCounts(
                        context.Options.Value, 0).Minimum,
                MaxPublishWorkerCount =
                    ManagedSessionOptionsAdapter.GetPublishWorkerCounts(
                        context.Options.Value, 0).Maximum,
                OperationLimitOverrides =
                    ManagedSessionOptionsAdapter.CreateOperationLimitOverrides(
                        context.Options.Value),
                DisableComplexTypeLoading =
                    connection.Options.HasFlag(ConnectionOptions.NoComplexTypeSystem),
                PreloadComplexTypes =
                    !(context.Options.Value.DisableComplexTypePreloading ?? false),
                ReverseConnectManager = connection.IsReverseConnect() ?
                    context.ReverseConnectManager : null,
                ReverseConnectServerUri = null
            };
        }

        internal static TimeSpan GetConnectTimeout(int? connectTimeout,
            OpcUaClientOptions options)
        {
            if (connectTimeout is > 0)
            {
                return TimeSpan.FromMilliseconds(connectTimeout.Value);
            }
            if (options.DefaultConnectTimeoutDuration is { } defaultConnectTimeout &&
                defaultConnectTimeout > TimeSpan.Zero)
            {
                return defaultConnectTimeout;
            }
            if (options.DefaultServiceCallTimeoutDuration is { } defaultServiceCallTimeout &&
                defaultServiceCallTimeout > TimeSpan.Zero)
            {
                return defaultServiceCallTimeout;
            }
            return TimeSpan.FromMinutes(1);
        }

        private readonly IOpcUaEndpointSelector _endpointSelector;
        private readonly Func<ReverseConnectManager, Uri, CancellationToken,
            Task<ITransportWaitingConnection>> _reverseConnectionWaiter;
    }

    /// <summary>
    /// Production managed-session runtime strategy.
    /// </summary>
    internal sealed class ManagedSessionRuntimeStrategy : IOpcUaClientRuntimeStrategy
    {
        public ManagedSessionRuntimeStrategy(IManagedSessionProvider provider,
            ITelemetryContext telemetry, IManagedSessionRequestFactory? requestFactory = null,
            ManagedSessionPoolOptions? options = null, TimeProvider? timeProvider = null)
        {
            _pool = new ManagedSessionPool(provider, telemetry, options, timeProvider);
            _requestFactory = requestFactory;
        }

        public IOpcUaClientRuntime Create(OpcUaClientRuntimeContext context)
        {
            return new ManagedOpcUaClient(context, _pool,
                _requestFactory ?? new DefaultManagedSessionRequestFactory(
                    context.EndpointSelector));
        }

        public ValueTask DisposeAsync()
        {
            return _pool.DisposeAsync();
        }

        private readonly ManagedSessionPool _pool;
        private readonly IManagedSessionRequestFactory? _requestFactory;
    }

    /// <summary>
    /// Managed-session client runtime.
    /// </summary>
    internal sealed class ManagedOpcUaClient : IOpcUaClientRuntime, IOpcUaClientDiagnostics
    {
        public ManagedOpcUaClient(OpcUaClientRuntimeContext context, ManagedSessionPool pool,
            IManagedSessionRequestFactory requestFactory)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            _requestFactory = requestFactory ?? throw new ArgumentNullException(nameof(requestFactory));
            _logger = context.LoggerFactory.CreateLogger<ManagedOpcUaClient>();
            _lifetimeToken = _lifetimeCts.Token;
            _lastDiagnostics = new ChannelDiagnosticModel
            {
                Connection = context.Connection.Connection,
                TimeStamp = context.TimeProvider.GetUtcNow()
            };
            _diagnosticsDumper =
                context.Connection.Connection.Options.HasFlag(
                    ConnectionOptions.DumpDiagnostics)
                    ? DumpDiagnosticsPeriodicallyAsync(_lifetimeToken)
                    : null;
        }

        public ChannelDiagnosticModel LastDiagnostics
        {
            get
            {
                lock (_diagnosticsGate)
                {
                    return _lastDiagnostics;
                }
            }
        }
        public int BadPublishRequestCount =>
            GetDiagnosticSession()?.BadPublishRequestCount ?? 0;
        public int GoodPublishRequestCount =>
            GetDiagnosticSession()?.GoodPublishRequestCount ?? 0;
        public int OutstandingRequestCount =>
            GetDiagnosticSession()?.OutstandingRequestCount ?? 0;
        public int SubscriptionCount =>
            GetDiagnosticSession()?.ServerSubscriptionCount ??
                Volatile.Read(ref _subscriptionCount);
        public EndpointConnectivityState State => _state;
        public int ReconnectCount => Volatile.Read(ref _reconnectCount);
        public bool ReconnectTriggered => _state == EndpointConnectivityState.Connecting;
        public int ConnectCount => Volatile.Read(ref _connectCount);
        public int MinPublishRequestCount =>
            GetDiagnosticSession()?.MinPublishRequestCount ?? 0;
        public int KeepAliveCounter =>
            GetDiagnosticSession()?.KeepAliveCounter ?? 0;
        public int KeepAliveTotal =>
            GetDiagnosticSession()?.KeepAliveTotal ?? 0;
        internal bool DiagnosticsDumperEnabled => _diagnosticsDumper != null;

        public async Task<ISessionHandle> AcquireAsync(int? connectTimeout,
            int? serviceCallTimeout, CancellationToken ct)
        {
            ThrowIfDisposed();
            ISessionHandle? lease = null;
            try
            {
                lease = await AcquireLeaseAsync(
                    GetConnectTimeoutOverride(connectTimeout, serviceCallTimeout), ct)
                    .ConfigureAwait(false);
                AddRef();
                return new ManagedSessionHandle(lease, GetServiceCallTimeout(serviceCallTimeout),
                    Dispose);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested &&
                !_lifetimeToken.IsCancellationRequested)
            {
                lease?.Dispose();
                throw new TimeoutException(
                    "Connecting to the managed OPC UA session timed out.");
            }
            catch
            {
                lease?.Dispose();
                throw;
            }
        }

        public async Task<T> RunAsync<T>(Func<ServiceCallContext, Task<T>> service,
            int? connectTimeout, int? serviceCallTimeout, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(service);
            ThrowIfDisposed();
            var timeout = GetServiceCallTimeout(serviceCallTimeout);
            ISessionHandle? lease = await AcquireLeaseAsync(
                GetConnectTimeoutOverride(connectTimeout, serviceCallTimeout), ct)
                .ConfigureAwait(false);
            using var call = CancellationTokenSource.CreateLinkedTokenSource(
                ct, _lifetimeToken);
            call.CancelAfter(timeout);
            try
            {
                using var context = new ServiceCallContext(lease.Session, timeout, call.Token);
                var result = await service(context).ConfigureAwait(false);
                CompleteServiceCall(context, ref lease);
                return result;
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested &&
                !_lifetimeToken.IsCancellationRequested)
            {
                throw new TimeoutException("The request operation timed out.");
            }
            finally
            {
                lease?.Dispose();
            }
        }

        public IAsyncEnumerable<T> RunAsync<T>(AsyncEnumerableBase<T> operation,
            int? connectTimeout, int? serviceCallTimeout, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(operation);
            return RunAsyncCore(operation, connectTimeout, serviceCallTimeout, ct);
        }

        public async ValueTask<PublisherSubscription> RegisterAsync(SubscriptionModel subscription,
            ISubscriber subscriber, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(subscription);
            ArgumentNullException.ThrowIfNull(subscriber);
            ThrowIfDisposed();

            using var operation = CancellationTokenSource.CreateLinkedTokenSource(
                ct, _lifetimeToken);
            await _subscriptionGate.WaitAsync(operation.Token).ConfigureAwait(false);
            ManagedSubscriptionState? state = null;
            ManagedRegistration? registration = null;
            var referenceAdded = false;
            try
            {
                ThrowIfDisposed();
                if (_registrations.TryGetValue(subscriber, out var existing))
                {
                    return await ReplaceRegistrationAsync(existing, subscription,
                        subscriber, ct, operation.Token).ConfigureAwait(false);
                }

                state = await GetOrCreateSubscriptionStateAsync(subscription,
                    operation.Token).ConfigureAwait(false);

                registration = new ManagedRegistration(this, state, subscriber);
                state.Registrations.Add(registration);
                _registrations.Add(subscriber, registration);
                AddRef();
                referenceAdded = true;
                await SynchronizeAsync(state, ct, _lifetimeToken).ConfigureAwait(false);
                return registration;
            }
            catch (Exception registrationException)
            {
                Exception? cleanupException = null;
                try
                {
                    if (registration != null)
                    {
                        await RemoveFailedRegistrationAsync(registration).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    cleanupException = ex;
                }
                finally
                {
                    if (referenceAdded)
                    {
                        Dispose();
                    }
                }
                if (cleanupException != null)
                {
                    throw new AggregateException(
                        "Managed subscription registration and cleanup failed.",
                        registrationException, cleanupException);
                }
                throw;
            }
            finally
            {
                _subscriptionGate.Release();
            }
        }

        public async Task ResetAsync(CancellationToken ct)
        {
            ThrowIfDisposed();
            using var operation = CancellationTokenSource.CreateLinkedTokenSource(
                ct, _lifetimeToken);
            await _subscriptionGate.WaitAsync(operation.Token).ConfigureAwait(false);
            ISessionHandle? temporaryLease = null;
            try
            {
                ThrowIfDisposed();
                var states = _subscriptions.Values.ToArray();
                var session = states.FirstOrDefault()?.Lease.Session as ManagedOpcUaSession;
                if (session == null)
                {
                    temporaryLease = await AcquireLeaseAsync(null, operation.Token)
                        .ConfigureAwait(false);
                    session = temporaryLease.Session as ManagedOpcUaSession ??
                        throw new InvalidOperationException(
                            "The managed pool returned a non-managed session facade.");
                }

                foreach (var state in states)
                {
                    state.Adapter.NotifyConnectionState(disconnected: true);
                }
                await session.ReconnectAsync(operation.Token).ConfigureAwait(false);
                UpdateDiagnostics(session);
                foreach (var state in states)
                {
                    await SynchronizeAsync(state, ct, _lifetimeToken).ConfigureAwait(false);
                }
                foreach (var state in states)
                {
                    state.Adapter.NotifyConnectionState(disconnected: false);
                }
            }
            finally
            {
                temporaryLease?.Dispose();
                _subscriptionGate.Release();
            }
        }

        public async Task<SessionDiagnosticsModel?> GetSessionDiagnosticsAsync(CancellationToken ct)
        {
            ManagedOpcUaSession? session;
            ManagedSessionDiagnosticsModel? managed;
            using var operation = CancellationTokenSource.CreateLinkedTokenSource(
                ct, _lifetimeToken);
            await _subscriptionGate.WaitAsync(operation.Token).ConfigureAwait(false);
            try
            {
                ThrowIfDisposed();
                session = _subscriptions.Values.FirstOrDefault()?.Lease.Session
                    as ManagedOpcUaSession ?? GetDiagnosticSession();
                managed = session == null ? null :
                    CreateManagedDiagnostics(session);
            }
            finally
            {
                _subscriptionGate.Release();
            }
            if (session == null)
            {
                return null;
            }
            var diagnostics = await session.GetServerDiagnosticAsync(ct)
                .ConfigureAwait(false);
            return diagnostics with
            {
                Managed = managed
            };
        }

        public async ValueTask CloseAsync(bool shutdown = false,
            bool fromManagementLoop = false)
        {
            lock (_lifetimeGate)
            {
                _closing = true;
            }
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }
            await _lifetimeCts.CancelAsync().ConfigureAwait(false);
            if (_diagnosticsDumper != null)
            {
                await _diagnosticsDumper.ConfigureAwait(false);
            }
            await _subscriptionGate.WaitAsync().ConfigureAwait(false);
            try
            {
                foreach (var state in _subscriptions.Values)
                {
                    await state.DisposeAsync().ConfigureAwait(false);
                }
                _subscriptions.Clear();
                Interlocked.Exchange(ref _subscriptionCount, 0);
                _registrations.Clear();
                foreach (var continuation in _continuations.ToArray())
                {
                    if (_continuations.TryRemove(continuation.Key, out var lease))
                    {
                        lease.Dispose();
                    }
                }
                lock (_observedSessions)
                {
                    foreach (var session in _observedSessions)
                    {
                        session.OnConnectionStateChange -= OnConnectionStateChanged;
                    }
                    _observedSessions.Clear();
                }
                lock (_diagnosticsGate)
                {
                    _diagnosticSession = null;
                }
                _state = EndpointConnectivityState.Disconnected;
            }
            finally
            {
                _subscriptionGate.Release();
                _lifetimeCts.Dispose();
            }
        }

        public void AddRef(string? token = null, TimeSpan? expiresAfter = null)
        {
            ObjectDisposedException.ThrowIf(!TryAddRef(), this);
        }

        public bool TryAddRef()
        {
            lock (_lifetimeGate)
            {
                if (_disposed != 0 || _closing)
                {
                    return false;
                }
                _references++;
                return true;
            }
        }

        public void Dispose()
        {
            var close = false;
            lock (_lifetimeGate)
            {
                if (_references == 0)
                {
                    return;
                }
                _references--;
                if (_references == 0 && !_closing)
                {
                    _closing = true;
                    close = true;
                }
            }
            if (close)
            {
                _ = CloseAndNotifyAsync();
            }
        }

        private async IAsyncEnumerable<T> RunAsyncCore<T>(AsyncEnumerableBase<T> operation,
            int? connectTimeout, int? serviceCallTimeout,
            [EnumeratorCancellation] CancellationToken ct)
        {
            var timeout = GetServiceCallTimeout(serviceCallTimeout);
            operation.Reset();
            while (operation.HasMore)
            {
                ThrowIfDisposed();
                ISessionHandle? lease = await AcquireLeaseAsync(
                    GetConnectTimeoutOverride(connectTimeout, serviceCallTimeout), ct)
                    .ConfigureAwait(false);
                using var call = CancellationTokenSource.CreateLinkedTokenSource(
                    ct, _lifetimeToken);
                call.CancelAfter(timeout);
                IEnumerable<T> results;
                try
                {
                    using var context = new ServiceCallContext(lease.Session, timeout, call.Token);
                    results = await operation.ExecuteAsync(context).ConfigureAwait(false);
                    CompleteServiceCall(context, ref lease);
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested &&
                    !_lifetimeToken.IsCancellationRequested)
                {
                    throw new TimeoutException("The request operation timed out.");
                }
                finally
                {
                    lease?.Dispose();
                }
                foreach (var result in results)
                {
                    yield return result;
                }
            }
        }

        private async Task<ISessionHandle> AcquireLeaseAsync(int? connectTimeout,
            CancellationToken ct)
        {
            var context = new ManagedSessionClientContext
            {
                Configuration = _context.Configuration,
                Connection = _context.Connection,
                Logger = _logger,
                Options = _context.ClientOptions,
                ReverseConnectManager = _context.ReverseConnectManager,
                TimeProvider = _context.TimeProvider,
                ConnectTimeout = connectTimeout
            };
            var requestFactory = _requestFactory;
            var timeout = DefaultManagedSessionRequestFactory.GetConnectTimeout(
                connectTimeout, _context.ClientOptions.Value);
            using var caller = CancellationTokenSource.CreateLinkedTokenSource(
                ct, _lifetimeToken);
            caller.CancelAfter(timeout);
            ISessionHandle lease;
            try
            {
                lease = await _pool.AcquireAsync(_context.Connection, timeout,
                    token => requestFactory.CreateAsync(context, token), caller.Token)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested &&
                !_lifetimeToken.IsCancellationRequested)
            {
                throw new TimeoutException(
                    "Connecting to the managed OPC UA session timed out.");
            }
            caller.CancelAfter(timeout);
            try
            {
                if (lease.Session is ManagedOpcUaSession session)
                {
                    await session.WaitForComplexTypePreloadAsync(caller.Token)
                        .ConfigureAwait(false);
                    if (!session.ComplexTypeLoadingDisabled)
                    {
                        _ = await session.GetComplexTypeSystemAsync(caller.Token)
                            .ConfigureAwait(false);
                    }
                    lock (_observedSessions)
                    {
                        if (_observedSessions.Add(session))
                        {
                            session.OnConnectionStateChange += OnConnectionStateChanged;
                        }
                    }
                    UpdateDiagnostics(session);
                }
                return lease;
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested &&
                !_lifetimeToken.IsCancellationRequested)
            {
                lease.Dispose();
                throw new TimeoutException(
                    "Managed OPC UA session readiness timed out.");
            }
            catch
            {
                lease.Dispose();
                throw;
            }
        }

        private void CompleteServiceCall(ServiceCallContext context,
            ref ISessionHandle? lease)
        {
            if (!string.IsNullOrEmpty(context.TrackedToken))
            {
                if (string.Equals(context.TrackedToken, context.UntrackedToken,
                    StringComparison.Ordinal))
                {
                    RenewContinuation(context.TrackedToken);
                }
                else if (lease != null)
                {
                    TrackContinuation(context.TrackedToken, lease);
                    lease = null;
                }
            }
            if (!string.IsNullOrEmpty(context.UntrackedToken) &&
                !string.Equals(context.UntrackedToken, context.TrackedToken,
                    StringComparison.Ordinal))
            {
                ReleaseContinuation(context.UntrackedToken);
            }
        }

        private void TrackContinuation(string token, ISessionHandle lease)
        {
            AddRef();
            var continuation = new ContinuationLease(this, token, lease,
                kContinuationTimeout);
            while (true)
            {
                if (_continuations.TryGetValue(token, out var existing))
                {
                    if (_continuations.TryUpdate(token, continuation, existing))
                    {
                        existing.Dispose();
                        return;
                    }
                    continue;
                }
                if (_continuations.TryAdd(token, continuation))
                {
                    return;
                }
            }
        }

        private void RenewContinuation(string token)
        {
            if (_continuations.TryGetValue(token, out var continuation))
            {
                continuation.Renew(kContinuationTimeout);
            }
        }

        private void ReleaseContinuation(string token, ContinuationLease? expected = null)
        {
            if (_continuations.TryGetValue(token, out var continuation) &&
                (expected == null || ReferenceEquals(expected, continuation)) &&
                ((ICollection<KeyValuePair<string, ContinuationLease>>)_continuations).Remove(
                    new KeyValuePair<string, ContinuationLease>(token, continuation)))
            {
                continuation.Dispose();
            }
        }

        private static async Task SynchronizeAsync(ManagedSubscriptionState state,
            CancellationToken ct, CancellationToken lifetimeCt)
        {
            await SynchronizeAdapterAsync(state.Adapter, state.Registrations,
                ct, lifetimeCt).ConfigureAwait(false);
        }

        private static async Task SynchronizeAdapterAsync(
            ManagedSubscriptionAdapter adapter,
            IEnumerable<ManagedRegistration> registrations,
            CancellationToken ct, CancellationToken lifetimeCt)
        {
            await adapter.UpdateAsync(registrations.SelectMany(registration =>
                registration.Owner.MonitoredItems.Select(item =>
                    (registration.Owner, Template: item))), ct, lifetimeCt).ConfigureAwait(false);
        }

        private async ValueTask<ManagedSubscriptionState>
            GetOrCreateSubscriptionStateAsync(SubscriptionModel subscription,
                CancellationToken ct)
        {
            if (_subscriptions.TryGetValue(subscription, out var existing))
            {
                return existing;
            }

            var lease = await AcquireLeaseAsync(null, ct).ConfigureAwait(false);
            ManagedSubscriptionState? state = null;
            var registered = false;
            try
            {
                if (lease.Session is not ManagedOpcUaSession session)
                {
                    throw new InvalidOperationException(
                        "The managed pool returned a non-managed session facade.");
                }
                if (!session.TryGetSubscriptionManager(out var manager))
                {
                    throw new InvalidOperationException(
                        "The managed session does not expose a subscription manager.");
                }
                state = new ManagedSubscriptionState(subscription, lease,
                    CreateSubscriptionAdapter(session, manager!, subscription));
                lease = null!;
                _subscriptions.Add(subscription, state);
                registered = true;
                Interlocked.Increment(ref _subscriptionCount);
                UpdatePublishWorkerCounts(session);
                return state;
            }
            catch (Exception creationException)
            {
                if (state == null)
                {
                    throw;
                }
                try
                {
                    if (registered)
                    {
                        await RemoveEmptyStateAsync(state).ConfigureAwait(false);
                    }
                    else
                    {
                        await state.DisposeAsync().ConfigureAwait(false);
                    }
                }
                catch (Exception cleanupException)
                {
                    throw new AggregateException(
                        "Managed subscription state creation and cleanup failed.",
                        creationException, cleanupException);
                }
                throw;
            }
            finally
            {
                lease?.Dispose();
            }
        }

        private ManagedSubscriptionAdapter CreateSubscriptionAdapter(
            ManagedOpcUaSession session, ISubscriptionManager manager,
            SubscriptionModel template)
        {
            return new ManagedSubscriptionAdapter(manager, template,
                _context.SubscriptionOptions.Value, session.Codec,
                (period, name) => session.CreateBrowser(period, name, _logger),
                _context.LoggerFactory.CreateLogger<ManagedSubscriptionAdapter>(),
                _context.TimeProvider,
                watchdogAction: HandleWatchdogAction,
                cyclicReadClient: new ManagedCyclicReadClient(
                    session, _context.TimeProvider,
                    _context.LoggerFactory.CreateLogger<ManagedCyclicReadClient>()),
                endpointUrlProvider: () => session.Endpoint.EndpointUrl?.ToString(),
                applicationUriProvider: () =>
                    session.InnerSession.Endpoint.Server.ApplicationUri ??
                    _context.Configuration.ApplicationUri,
                monitoredItemPreparation: async (template, ct) =>
                {
                    var eventTypeDefinitionId =
                        (template as EventMonitoredItemModel)?.EventFilter.TypeDefinitionId;
                    if (template.RelativePath is { Count: > 0 })
                    {
                        var startingNode = template.StartNodeId.ToNodeId(
                            session.MessageContext);
                        if (Opc.Ua.NodeIdCompat.IsNull(startingNode))
                        {
                            throw new ServiceResultException(
                                StatusCodes.BadNodeIdInvalid,
                                $"Invalid monitored-item start node '{template.StartNodeId}'.");
                        }
                        var response = await session.Services
                            .TranslateBrowsePathsToNodeIdsAsync(new RequestHeader(),
                                new BrowsePathCollection
                                {
                                    new BrowsePath
                                    {
                                        StartingNode = startingNode,
                                        RelativePath = template.RelativePath.ToRelativePath(
                                            session.MessageContext)
                                    }
                                }, ct).ConfigureAwait(false);
                        if (StatusCode.IsBad(response.ResponseHeader.ServiceResult) ||
                            response.Results.Count != 1 ||
                            StatusCode.IsBad(response.Results[0].StatusCode) ||
                            response.Results[0].Targets.Count != 1)
                        {
                            var statusCode = response.Results.Count == 1 ?
                                response.Results[0].StatusCode :
                                response.ResponseHeader.ServiceResult;
                            throw ServiceResultException.Create(statusCode,
                                $"Failed to resolve monitored-item path from " +
                                $"'{template.StartNodeId}'.");
                        }
                        var resolvedNodeId = ExpandedNodeId.ToNodeId(
                            response.Results[0].Targets[0].TargetId,
                            session.MessageContext.NamespaceUris);
                        template = template with
                        {
                            StartNodeId = resolvedNodeId.AsString(session.MessageContext,
                                template.NamespaceFormat) ?? string.Empty,
                            RelativePath = null
                        };
                    }
                    if (template is EventMonitoredItemModel events &&
                        !string.IsNullOrEmpty(eventTypeDefinitionId))
                    {
                        var filter = await OpcUaMonitoredItem.Event
                            .CreateSimpleEventFilterAsync(events with
                            {
                                EventFilter = events.EventFilter with
                                {
                                    TypeDefinitionId = eventTypeDefinitionId
                                }
                            }, session, ct)
                            .ConfigureAwait(false);
                        template = events with
                        {
                            EventFilter = session.Codec.Encode(filter,
                                events.NamespaceFormat)!
                        };
                    }
                    if (template.FetchDataSetFieldName != true)
                    {
                        return template;
                    }
                    var displayNodeId = template is EventMonitoredItemModel ?
                        eventTypeDefinitionId : template.StartNodeId;
                    if (string.IsNullOrEmpty(displayNodeId))
                    {
                        return template;
                    }
                    var nodeId = displayNodeId.ToNodeId(session.MessageContext);
                    if (Opc.Ua.NodeIdCompat.IsNull(nodeId))
                    {
                        return template;
                    }
                    var node = await session.LruNodeCache.GetNodeAsync(nodeId, ct)
                        .ConfigureAwait(false);
                    return template with
                    {
                        DataSetFieldName = node?.DisplayName.ToString() ?? string.Empty
                    };
                },
                browsePathFromRootResolver: async (nodeIdValue, ct) =>
                {
                    var nodeId = nodeIdValue.ToNodeId(session.MessageContext);
                    if (Opc.Ua.NodeIdCompat.IsNull(nodeId))
                    {
                        return null;
                    }
                    var paths = await session.GetBrowsePathsFromRootAsync(
                        new RequestHeader(), new[] { nodeId }, ct).ConfigureAwait(false);
                    return paths.Count == 0 || paths[0].ErrorInfo != null ?
                        null : paths[0].Path;
                });
        }

        private async ValueTask<ManagedRegistration> ReplaceRegistrationAsync(
            ManagedRegistration existing, SubscriptionModel subscription,
            ISubscriber subscriber, CancellationToken ct, CancellationToken operationCt)
        {
            if (_subscriptions.TryGetValue(subscription, out var targetState) &&
                ReferenceEquals(targetState, existing.State))
            {
                var index = targetState.Registrations.IndexOf(existing);
                if (index < 0)
                {
                    throw new InvalidOperationException(
                        "The existing managed registration is not tracked by its state.");
                }
                var replacement = new ManagedRegistration(this, targetState, subscriber);
                targetState.Registrations[index] = replacement;
                try
                {
                    await SynchronizeAsync(targetState, ct, _lifetimeToken)
                        .ConfigureAwait(false);
                }
                catch
                {
                    targetState.Registrations[index] = existing;
                    throw;
                }
                _registrations.Remove(existing.Owner);
                _registrations.Add(subscriber, replacement);
                return replacement;
            }

            targetState = await GetOrCreateSubscriptionStateAsync(subscription, operationCt)
                .ConfigureAwait(false);
            var targetWasEmpty = targetState.Registrations.Count == 0;
            var provisional = new ManagedRegistration(this, targetState, subscriber);
            targetState.Registrations.Add(provisional);
            try
            {
                await SynchronizeAsync(targetState, ct, _lifetimeToken).ConfigureAwait(false);
            }
            catch (Exception synchronizationException)
            {
                await RemoveProvisionalRegistrationAsync(provisional,
                    resynchronize: false, synchronizationException).ConfigureAwait(false);
                throw;
            }

            Exception? cleanupException;
            try
            {
                cleanupException = await RemoveRegistrationAsync(existing, ct,
                    releaseReference: false)
                    .ConfigureAwait(false);
            }
            catch (Exception removalException)
            {
                await RemoveProvisionalRegistrationAsync(provisional,
                    resynchronize: !targetWasEmpty, removalException).ConfigureAwait(false);
                throw;
            }

            _registrations.Add(subscriber, provisional);
            if (cleanupException != null)
            {
                _logger.LogError(cleanupException,
                    "Managed subscription cleanup failed after replacement committed.");
            }
            return provisional;
        }

        private async ValueTask RemoveProvisionalRegistrationAsync(
            ManagedRegistration registration, bool resynchronize,
            Exception originalException)
        {
            var state = registration.State;
            state.Registrations.Remove(registration);
            try
            {
                if (state.Registrations.Count == 0)
                {
                    await RemoveEmptyStateAsync(state).ConfigureAwait(false);
                }
                else if (resynchronize)
                {
                    await SynchronizeAsync(state, default, _lifetimeToken)
                        .ConfigureAwait(false);
                }
            }
            catch (Exception cleanupException)
            {
                throw new AggregateException(
                    "Managed replacement registration and cleanup failed.",
                    originalException, cleanupException);
            }
        }

        private async ValueTask<Exception?> RemoveRegistrationAsync(
            ManagedRegistration registration,
            CancellationToken ct, bool releaseReference = true)
        {
            var state = registration.State;
            var registrationIndex = state.Registrations.IndexOf(registration);
            if (registrationIndex < 0 ||
                !_registrations.TryGetValue(registration.Owner, out var current) ||
                !ReferenceEquals(current, registration))
            {
                return null;
            }
            _registrations.Remove(registration.Owner);
            state.Registrations.RemoveAt(registrationIndex);
            if (state.Registrations.Count == 0)
            {
                Exception? cleanupException = null;
                try
                {
                    await RemoveEmptyStateAsync(state).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    cleanupException = ex;
                }
                finally
                {
                    if (releaseReference)
                    {
                        Dispose();
                    }
                }
                if (releaseReference && cleanupException != null)
                {
                    ExceptionDispatchInfo.Capture(cleanupException).Throw();
                }
                return cleanupException;
            }
            try
            {
                await SynchronizeAsync(state, ct, _lifetimeToken).ConfigureAwait(false);
            }
            catch
            {
                state.Registrations.Insert(registrationIndex, registration);
                _registrations.Add(registration.Owner, registration);
                throw;
            }
            if (releaseReference)
            {
                Dispose();
            }
            return null;
        }

        private async ValueTask RemoveFailedRegistrationAsync(
            ManagedRegistration registration)
        {
            if (_registrations.TryGetValue(registration.Owner, out var current) &&
                ReferenceEquals(current, registration))
            {
                _registrations.Remove(registration.Owner);
            }
            var state = registration.State;
            state.Registrations.Remove(registration);
            if (state.Registrations.Count == 0)
            {
                await RemoveEmptyStateAsync(state).ConfigureAwait(false);
            }
        }

        private async ValueTask RemoveEmptyStateAsync(ManagedSubscriptionState state)
        {
            _subscriptions.Remove(state.Template);
            Interlocked.Decrement(ref _subscriptionCount);
            Exception? workerException = null;
            if (state.Lease.Session is ManagedOpcUaSession session)
            {
                try
                {
                    UpdatePublishWorkerCounts(session);
                }
                catch (Exception ex)
                {
                    workerException = ex;
                }
            }
            try
            {
                await state.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception disposeException) when (workerException != null)
            {
                throw new AggregateException(
                    "Managed subscription worker update and disposal failed.",
                    workerException, disposeException);
            }
            if (workerException != null)
            {
                throw workerException;
            }
        }

        private async ValueTask DisposeRegistrationAsync(ManagedRegistration registration)
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                return;
            }
            try
            {
                await _subscriptionGate.WaitAsync(_lifetimeToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_lifetimeToken.IsCancellationRequested)
            {
                return;
            }
            try
            {
                if (Volatile.Read(ref _disposed) != 0)
                {
                    return;
                }
                if (_registrations.TryGetValue(registration.Owner, out var current) &&
                    ReferenceEquals(current, registration))
                {
                    await RemoveRegistrationAsync(registration, default).ConfigureAwait(false);
                }
            }

            finally
            {
                _subscriptionGate.Release();
            }
        }

        private async Task SynchronizeRegistrationAsync(ManagedRegistration registration)
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                return;
            }
            var entered = false;
            try
            {
                await _subscriptionGate.WaitAsync(_lifetimeToken).ConfigureAwait(false);
                entered = true;
                if (Volatile.Read(ref _disposed) == 0 &&
                    _registrations.TryGetValue(registration.Owner, out var current) &&
                    ReferenceEquals(current, registration))
                {
                    await SynchronizeAsync(registration.State, default, _lifetimeToken)
                        .ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                if (Volatile.Read(ref _disposed) == 0)
                {
                    _logger.LogError(ex, "Managed subscription synchronization failed.");
                }
            }
            finally
            {
                if (entered)
                {
                    _subscriptionGate.Release();
                }
            }
        }

        private async Task CloseAndNotifyAsync()
        {
            await CloseAsync().ConfigureAwait(false);
            await _context.OnClose().ConfigureAwait(false);
        }

        private void UpdatePublishWorkerCounts(ManagedOpcUaSession session)
        {
            if (!session.TryGetSubscriptionManager(out var manager))
            {
                throw new InvalidOperationException(
                    "The managed session does not expose a subscription manager.");
            }
            var counts = ManagedSessionOptionsAdapter.GetPublishWorkerCounts(
                _context.ClientOptions.Value, Volatile.Read(ref _subscriptionCount));
            manager!.MinPublishWorkerCount = counts.Minimum;
            manager.MaxPublishWorkerCount = counts.Maximum;
        }

        private void OnConnectionStateChanged(object? sender,
            EndpointConnectivityStateEventArgs e)
        {
            _state = e.State;
            var stateVersion = Interlocked.Increment(ref _connectionStateVersion);
            _ = NotifySubscriptionConnectionStateAsync(
                e.State != EndpointConnectivityState.Ready, stateVersion);
            if (e.State == EndpointConnectivityState.Ready)
            {
                Interlocked.Increment(ref _connectCount);
                if (sender is ManagedOpcUaSession session)
                {
                    UpdateDiagnostics(session);
                }
            }
            else if (e.State == EndpointConnectivityState.Connecting)
            {
                Interlocked.Increment(ref _reconnectCount);
            }
            try
            {
                _context.Notifier?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Managed session state callback failed.");
            }
        }

        private async Task NotifySubscriptionConnectionStateAsync(
            bool disconnected, long stateVersion)
        {
            var entered = false;
            try
            {
                await _subscriptionGate.WaitAsync(_lifetimeToken).ConfigureAwait(false);
                entered = true;
                if (Volatile.Read(ref _disposed) != 0 ||
                    Volatile.Read(ref _connectionStateVersion) != stateVersion)
                {
                    return;
                }
                foreach (var state in _subscriptions.Values)
                {
                    state.Adapter.NotifyConnectionState(disconnected);
                }
            }

            catch (OperationCanceledException) when (_lifetimeToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                if (Volatile.Read(ref _disposed) == 0)
                {
                    _logger.LogError(ex,
                        "Managed heartbeat connection-state update failed.");
                }
            }
            finally
            {
                if (entered)
                {
                    _subscriptionGate.Release();
                }
            }
        }

        private void HandleWatchdogAction(ManagedSubscriptionAdapter adapter,
            SubscriptionWatchdogBehavior behavior, string message)
        {
            lock (_lifetimeGate)
            {
                if (_disposed != 0 || _closing)
                {
                    return;
                }
            }
            switch (behavior)
            {
                case SubscriptionWatchdogBehavior.Diagnostic:
                    _logger.LogWarning("{Message}", message);
                    break;
                case SubscriptionWatchdogBehavior.Reset:
                    StartWatchdogReset(adapter, message);
                    break;
                case SubscriptionWatchdogBehavior.FailFast:
                    Publisher.Runtime.FailFast(message, null);
                    break;
                case SubscriptionWatchdogBehavior.ExitProcess:
                    Console.WriteLine(message);
                    Publisher.Runtime.Exit(-10);
                    break;
            }
        }

        private void StartWatchdogReset(
            ManagedSubscriptionAdapter adapter, string message)
        {
            _ = RunWithoutExecutionContextAsync(
                () => ResetFromWatchdogAsync(adapter, message));
        }

        internal static Task RunWithoutExecutionContextAsync(Func<Task> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            if (ExecutionContext.IsFlowSuppressed())
            {
                return Task.Run(action);
            }
            using (ExecutionContext.SuppressFlow())
            {
                return Task.Run(action);
            }
        }

        private async Task ResetFromWatchdogAsync(
            ManagedSubscriptionAdapter adapter, string message)
        {
            var succeeded = false;
            try
            {
                _logger.LogWarning("{Message}", message);
                var attempt = 0;
                while (!_lifetimeToken.IsCancellationRequested)
                {
                    var entered = false;
                    try
                    {
                        await _subscriptionGate.WaitAsync(_lifetimeToken)
                            .ConfigureAwait(false);
                        entered = true;
                        var state = _subscriptions.Values.FirstOrDefault(candidate =>
                            ReferenceEquals(candidate.Adapter, adapter));
                        if (Volatile.Read(ref _disposed) != 0 || state == null)
                        {
                            return;
                        }
                        using var reset =
                            CancellationTokenSource.CreateLinkedTokenSource(
                                _lifetimeToken);
                        reset.CancelAfter(GetServiceCallTimeout(null));
                        await adapter.Subscription.RecreateAsync(reset.Token)
                            .ConfigureAwait(false);
                        await SynchronizeAsync(state, default, reset.Token)
                            .ConfigureAwait(false);
                        succeeded = true;
                        return;
                    }
                    catch (OperationCanceledException)
                        when (!_lifetimeToken.IsCancellationRequested)
                    {
                        _logger.LogWarning(
                            "Managed watchdog reset timed out; retrying.");
                    }
                    catch (Exception ex)
                    {
                        if (Volatile.Read(ref _disposed) == 0)
                        {
                            _logger.LogError(ex,
                                "Managed watchdog reset failed; retrying.");
                        }
                    }
                    finally
                    {
                        if (entered)
                        {
                            _subscriptionGate.Release();
                        }
                    }

                    attempt++;
                    var delay = TimeSpan.FromSeconds(Math.Min(30,
                        1 << Math.Min(attempt - 1, 5)));
                    await Task.Delay(delay, _context.TimeProvider,
                        _lifetimeToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (_lifetimeToken.IsCancellationRequested)
            {
            }
            finally
            {
                adapter.CompleteWatchdogReset(succeeded);
            }
        }

        internal async Task DumpDiagnosticsAsync(TextWriter writer,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(writer);
            var diagnostics = await GetSessionDiagnosticsAsync(ct)
                .ConfigureAwait(false);
            if (diagnostics == null)
            {
                return;
            }
            var json = Json.SerializeToString(diagnostics,
                Json.GetTypeInfo<SessionDiagnosticsModel>(),
                SerializeOption.Indented);
            await writer.WriteLineAsync(json.AsMemory(), ct).ConfigureAwait(false);
            Volatile.Write(ref _diagnosticsDumpError, null);
        }

        private async Task DumpDiagnosticsPeriodicallyAsync(CancellationToken ct)
        {
            using var timer = new PeriodicTimer(
                kDiagnosticsDumpInterval, _context.TimeProvider);
            try
            {
                while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
                {
                    try
                    {
                        await DumpDiagnosticsAsync(Console.Out, ct)
                            .ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Volatile.Write(ref _diagnosticsDumpError, ex);
                        _logger.LogWarning(ex,
                            "Managed diagnostic dump failed.");
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
            }
        }

        private ManagedSessionDiagnosticsModel CreateManagedDiagnostics(
            ManagedOpcUaSession session)
        {
            var subscriptions = _subscriptions.Values
                .Select(CreateManagedSubscriptionDiagnostics)
                .ToArray();
            var errors = subscriptions
                .SelectMany(subscription => subscription.BackgroundErrors ?? [])
                .ToList();
            if (session.ComplexTypePreloadError is { } complexTypeError)
            {
                errors.Add(complexTypeError.ToString());
            }
            if (Volatile.Read(ref _diagnosticsDumpError) is { } dumpError)
            {
                errors.Add(dumpError.ToString());
            }
            return new ManagedSessionDiagnosticsModel
            {
                State = State,
                ConnectCount = ConnectCount,
                ReconnectCount = ReconnectCount,
                ReconnectTriggered = ReconnectTriggered,
                PublishWorkerCount = session.PublishWorkerCount,
                GoodPublishRequestCount = session.GoodPublishRequestCount,
                BadPublishRequestCount = session.BadPublishRequestCount,
                OutstandingRequestCount = session.OutstandingRequestCount,
                MinimumPublishRequestCount = session.MinPublishRequestCount,
                KeepAliveCounter = session.KeepAliveCounter,
                KeepAliveTotal = session.KeepAliveTotal,
                ComplexTypeSystemLoaded = session.IsComplexTypeSystemLoaded,
                ComplexTypeSystemFullyLoaded =
                    session.IsComplexTypeSystemFullyLoaded,
                BackgroundErrors = errors.Count == 0
                    ? null
                    : errors.Distinct(StringComparer.Ordinal).ToArray(),
                Subscriptions = subscriptions.Length == 0
                    ? null
                    : subscriptions
            };
        }

        private static ManagedSubscriptionDiagnosticsModel
            CreateManagedSubscriptionDiagnostics(ManagedSubscriptionState state)
        {
            var snapshots = state.Registrations
                .Select(registration =>
                    state.Adapter.GetDiagnostics(registration.Owner))
                .ToArray();
            var first = snapshots.FirstOrDefault();
            var ids = state.Adapter.Subscription is
                IPartitionedSubscription partitioned
                    ? partitioned.PartitionIds.ToArray()
                    : [];
            var errors = state.Adapter.GetBackgroundErrors()
                .Select(error => error.ToString())
                .ToArray();
            return new ManagedSubscriptionDiagnosticsModel
            {
                SubscriptionIds = ids.Length == 0 ? null : ids,
                RegistrationCount = state.Registrations.Count,
                PartitionCount = first.PartitionCount != 0
                    ? first.PartitionCount
                    : state.Adapter.Subscription is IPartitionedSubscription value
                        ? value.PartitionCount
                        : 1,
                MonitoredItems = snapshots.Sum(item => item.MonitoredItems),
                AppliedMonitoredItems = snapshots.Sum(
                    item => item.AppliedMonitoredItems),
                PendingMonitoredItems = snapshots.Sum(
                    item => item.PendingMonitoredItems),
                RetryingMonitoredItems = snapshots.Sum(
                    item => item.RetryingMonitoredItems),
                TerminalMonitoredItems = snapshots.Sum(
                    item => item.TerminalMonitoredItems),
                CyclicMonitoredItems = snapshots.Sum(
                    item => item.CyclicMonitoredItems),
                CyclicWorkerCount = first.CyclicWorkerCount,
                RetryCount = state.Adapter.RetryCount,
                HeartbeatsEnabled = snapshots.Sum(item => item.HeartbeatsEnabled),
                ConditionsEnabled = snapshots.Sum(item => item.ConditionsEnabled),
                LateMonitoredItems = snapshots.Sum(item => item.LateMonitoredItems),
                PublishingEnabled = first.PublishingEnabled,
                WatchdogEnabled = first.WatchdogEnabled,
                WatchdogResetInProgress = first.WatchdogResetInProgress,
                BackgroundErrors = errors.Length == 0 ? null : errors
            };
        }

        private void UpdateDiagnostics(ManagedOpcUaSession session)
        {
            var diagnostics = new ChannelDiagnosticModel
            {
                Connection = _context.Connection.Connection,
                TimeStamp = _context.TimeProvider.GetUtcNow(),
                SessionCreated = session.CreatedAt,
                SessionId = session.SessionId.ToString()
            };
            lock (_diagnosticsGate)
            {
                _diagnosticSession = session;
                if (string.Equals(_lastDiagnostics.SessionId, diagnostics.SessionId,
                    StringComparison.Ordinal) &&
                    _lastDiagnostics.SessionCreated == diagnostics.SessionCreated)
                {
                    return;
                }
                _lastDiagnostics = diagnostics;
            }
            _context.DiagnosticsCallback(diagnostics);
        }

        private ManagedOpcUaSession? GetDiagnosticSession()
        {
            lock (_diagnosticsGate)
            {
                return _diagnosticSession;
            }
        }

        private TimeSpan GetServiceCallTimeout(int? serviceCallTimeout)
        {
            return serviceCallTimeout is > 0 ?
                TimeSpan.FromMilliseconds(serviceCallTimeout.Value) :
                _context.ClientOptions.Value.DefaultServiceCallTimeoutDuration ??
                TimeSpan.FromMinutes(5);
        }

        private int? GetConnectTimeoutOverride(int? connectTimeout,
            int? serviceCallTimeout)
        {
            if (connectTimeout is > 0 ||
                _context.ClientOptions.Value.DefaultConnectTimeoutDuration is { } configured &&
                    configured > TimeSpan.Zero)
            {
                return connectTimeout;
            }
            return serviceCallTimeout is > 0 ? serviceCallTimeout : connectTimeout;
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        }

        private sealed class ManagedSessionHandle : ISessionHandle
        {
            public ManagedSessionHandle(ISessionHandle inner, TimeSpan serviceCallTimeout,
                Action release)
            {
                _inner = inner;
                ServiceCallTimeout = serviceCallTimeout;
                _release = release;
            }

            public IOpcUaSession Session => _inner?.Session ??
                throw new ObjectDisposedException(nameof(ManagedSessionHandle));
            public TimeSpan ServiceCallTimeout { get; }

            public void Dispose()
            {
                var inner = Interlocked.Exchange(ref _inner, null);
                if (inner != null)
                {
                    inner.Dispose();
                    Interlocked.Exchange(ref _release, null)?.Invoke();
                }
            }

            private ISessionHandle? _inner;
            private Action? _release;
        }

        private sealed class ContinuationLease : IDisposable
        {
            public ContinuationLease(ManagedOpcUaClient owner, string token,
                ISessionHandle lease, TimeSpan timeout)
            {
                _owner = owner;
                _token = token;
                _lease = lease;
                _timer = owner._context.TimeProvider.CreateTimer(static state =>
                    ((ContinuationLease)state!).Expire(), this, timeout,
                    Timeout.InfiniteTimeSpan);
            }

            public void Renew(TimeSpan timeout)
            {
                _timer.Change(timeout, Timeout.InfiniteTimeSpan);
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0)
                {
                    return;
                }
                _timer.Dispose();
                _lease.Dispose();
                _owner.Dispose();
            }

            private void Expire()
            {
                _owner.ReleaseContinuation(_token, this);
            }

            private int _disposed;
            private readonly ManagedOpcUaClient _owner;
            private readonly ISessionHandle _lease;
            private readonly string _token;
            private readonly ITimer _timer;
        }

        private sealed class ManagedSubscriptionState : IAsyncDisposable
        {
            public ManagedSubscriptionState(SubscriptionModel template, ISessionHandle lease,
                ManagedSubscriptionAdapter adapter)
            {
                Template = template;
                Lease = lease;
                Adapter = adapter;
            }

            public ManagedSubscriptionAdapter Adapter { get; }
            public ISessionHandle Lease { get; }
            public List<ManagedRegistration> Registrations { get; } = [];
            public SubscriptionModel Template { get; }

            public async ValueTask DisposeAsync()
            {
                Exception? adapterException = null;
                try
                {
                    await Adapter.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    adapterException = ex;
                }
                try
                {
                    Lease.Dispose();
                }
                catch (Exception leaseException) when (adapterException != null)
                {
                    throw new AggregateException(
                        "Managed subscription adapter and lease disposal failed.",
                        adapterException, leaseException);
                }
                if (adapterException != null)
                {
                    ExceptionDispatchInfo.Capture(adapterException).Throw();
                }
            }
        }

        private sealed class ManagedRegistration : PublisherSubscription, ISubscriptionDiagnostics
        {
            public ManagedRegistration(ManagedOpcUaClient owner,
                ManagedSubscriptionState state, ISubscriber subscriber)
            {
                _owner = owner;
                State = state;
                Owner = subscriber;
            }

            public IOpcUaClientDiagnostics ClientDiagnostics => _owner;
            public ISubscriptionDiagnostics Diagnostics => this;
            public int GoodMonitoredItems =>
                State.Adapter.GetGoodMonitoredItems(Owner);
            public int BadMonitoredItems =>
                State.Adapter.GetBadMonitoredItems(Owner);
            public int LateMonitoredItems => State.Adapter.GetLateMonitoredItems(Owner);
            public int HeartbeatsEnabled => State.Adapter.GetHeartbeatsEnabled(Owner);
            public int ConditionsEnabled => State.Adapter.GetConditionsEnabled(Owner);
            public ISubscriber Owner { get; }
            public ManagedSubscriptionState State { get; }

            public ValueTask DisposeAsync()
            {
                return _owner.DisposeRegistrationAsync(this);
            }

            public OpcUaSubscriptionNotification? CreateKeepAlive()
            {
                return State.Adapter.CreateKeepAlive(Owner);
            }

            public void NotifyMonitoredItemsChanged()
            {
                _ = _owner.SynchronizeRegistrationAsync(this);
            }

            public ValueTask<PublishedDataSetMetaDataModel> CollectMetaDataAsync(
                ISubscriber owner, DataSetFieldContentFlags? fieldMask,
                DataSetMetaDataModel dataSetMetaData, uint minorVersion,
                CancellationToken ct = default)
            {
                return CollectMetaDataCoreAsync(owner, dataSetMetaData, minorVersion, ct);
            }

            private async ValueTask<PublishedDataSetMetaDataModel> CollectMetaDataCoreAsync(
                ISubscriber owner, DataSetMetaDataModel dataSetMetaData, uint minorVersion,
                CancellationToken ct)
            {
                ArgumentNullException.ThrowIfNull(owner);
                var session = State.Lease.Session as ManagedOpcUaSession ??
                    throw new InvalidOperationException(
                        "The managed subscription lease returned a non-managed session.");
                var typeSystem = await session.GetComplexTypeSystemAsync(ct)
                    .ConfigureAwait(false);
                var dataTypes = new NodeIdDictionary<object>();
                var fields = new List<PublishedFieldMetaDataModel>();
                var metadataBuilder = new MonitoredItemMetaDataBuilder(NullLogger.Instance);
                var dataItems = State.Adapter.GetDataMetadata(owner);
                foreach (var item in dataItems)
                {
                    await metadataBuilder.BuildDataChangeAsync(session, typeSystem,
                        item, fields, dataTypes, ct)
                        .ConfigureAwait(false);
                }
                var eventItems = State.Adapter.GetEventMetadata(owner);
                foreach (var item in eventItems)
                {
                    await metadataBuilder.BuildEventAsync(session, typeSystem,
                        item.Template, item.Filter, item.FieldNames, item.FieldIds,
                        fields, dataTypes, ct).ConfigureAwait(false);
                }
                return new PublishedDataSetMetaDataModel
                {
                    DataSetMetaData = dataSetMetaData,
                    EnumDataTypes = dataTypes.Values.OfType<EnumDescriptionModel>().ToList(),
                    StructureDataTypes = dataTypes.Values.OfType<StructureDescriptionModel>().ToList(),
                    SimpleDataTypes = dataTypes.Values.OfType<SimpleTypeDescriptionModel>().ToList(),
                    Fields = fields,
                    MinorVersion = minorVersion
                };

            }

            private readonly ManagedOpcUaClient _owner;
        }

        private const int kContinuationTimeoutMilliseconds = 10000;
        private static readonly TimeSpan kContinuationTimeout =
            TimeSpan.FromMilliseconds(kContinuationTimeoutMilliseconds);
        private static readonly TimeSpan kDiagnosticsDumpInterval =
            TimeSpan.FromSeconds(10);
        private long _connectionStateVersion;
        private int _connectCount;
        private int _disposed;
        private int _reconnectCount;
        private int _references;
        private int _subscriptionCount;
        private bool _closing;
        private Exception? _diagnosticsDumpError;
        private ManagedOpcUaSession? _diagnosticSession;
        private ChannelDiagnosticModel _lastDiagnostics;
        private readonly Task? _diagnosticsDumper;
        private EndpointConnectivityState _state = EndpointConnectivityState.Disconnected;
        private readonly OpcUaClientRuntimeContext _context;
        private readonly ILogger _logger;
        private readonly ManagedSessionPool _pool;
        private readonly IManagedSessionRequestFactory _requestFactory;
        private readonly ConcurrentDictionary<string, ContinuationLease> _continuations = [];
        private readonly Lock _diagnosticsGate = new();
        private readonly CancellationTokenSource _lifetimeCts = new();
        private readonly CancellationToken _lifetimeToken;
        private readonly Lock _lifetimeGate = new();
        private readonly HashSet<ManagedOpcUaSession> _observedSessions = [];
        private readonly Dictionary<ISubscriber, ManagedRegistration> _registrations = [];
#pragma warning disable CA2213 // Retained so pre-close synchronization tasks can release safely.
        private readonly SemaphoreSlim _subscriptionGate = new(1, 1);
#pragma warning restore CA2213 // Disposable fields should be disposed
        private readonly Dictionary<SubscriptionModel, ManagedSubscriptionState> _subscriptions = [];
    }
}
