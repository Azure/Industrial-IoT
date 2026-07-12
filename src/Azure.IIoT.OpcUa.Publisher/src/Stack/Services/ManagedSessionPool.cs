// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Opc.Ua;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Settings for the unused managed-session pool seam.
    /// </summary>
    internal sealed class ManagedSessionPoolOptions
    {
        /// <summary>
        /// Duration for which an unreferenced connection remains reusable.
        /// </summary>
        public TimeSpan LingerTimeout { get; init; } = TimeSpan.Zero;

        /// <summary>
        /// Default timeout assigned to leases.
        /// </summary>
        public TimeSpan ServiceCallTimeout { get; init; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Expiration interval for each facade node cache.
        /// </summary>
        public TimeSpan NodeCacheTimeout { get; init; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Maximum entries held by each facade node cache.
        /// </summary>
        public int NodeCacheCapacity { get; init; } = 4096;
    }

    /// <summary>
    /// Connection-identity keyed pool for <see cref="ManagedOpcUaSession"/>.
    /// </summary>
    /// <remarks>
    /// A lease owns a reference, never the session itself. When the final lease is
    /// released, the pool disposes the managed inner session after the configured
    /// linger period. Call cancellation cancels only that caller's wait; the shared
    /// connect uses the request timeout and remains available to other callers.
    /// </remarks>
    internal sealed class ManagedSessionPool : IAsyncDisposable
    {
        /// <summary>
        /// Create the pool.
        /// </summary>
        public ManagedSessionPool(IManagedSessionProvider provider,
            ITelemetryContext telemetry, ManagedSessionPoolOptions? options = null,
            TimeProvider? timeProvider = null)
        {
            _provider = provider ??
                throw new ArgumentNullException(nameof(provider));
            _telemetry = telemetry ??
                throw new ArgumentNullException(nameof(telemetry));
            _options = options ??
                new ManagedSessionPoolOptions();
            if (_options.LingerTimeout < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(options));
            }
            if (_options.ServiceCallTimeout <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(options));
            }
            if (_options.NodeCacheTimeout <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(options));
            }
            if (_options.NodeCacheCapacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options));
            }
            _timeProvider = timeProvider ??
                TimeProvider.System;
        }

        /// <summary>
        /// Acquire a reference-counted session lease.
        /// </summary>
        public async Task<ISessionHandle> AcquireAsync(
            ManagedSessionConnectionRequest request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            while (true)
            {
                ThrowIfDisposed();
                var entry = _sessions.GetOrAdd(request.Connection,
                    key => new Entry(this, key, request));
                try
                {
                    var lease = await entry.AcquireAsync(ct).ConfigureAwait(false);
                    if (Volatile.Read(ref _disposed) != 0)
                    {
                        lease.Dispose();
                        ThrowIfDisposed();
                    }
                    return lease;
                }
                catch (EntryClosingException)
                {
                    // A final release won the race with this acquire. Use the
                    // newly-created entry rather than reviving a closing session.
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    // Caller cancellation must not remove or close the shared connect.
                    throw;
                }
                catch
                {
                    if (Remove(entry))
                    {
                        await entry.CloseAsync().ConfigureAwait(false);
                    }
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            await _shutdown.CancelAsync().ConfigureAwait(false);
            var entries = _sessions.ToArray();
            _sessions.Clear();
            foreach (var entry in entries)
            {
                await entry.Value.CloseAsync().ConfigureAwait(false);
            }
            _shutdown.Dispose();
        }

        private async Task<ManagedOpcUaSession> ConnectAsync(
            ManagedSessionConnectionRequest request)
        {
            var attempt = new ConnectionAttempt(_provider, request, _shutdown.Token);
            var transferredOwnership = false;
            try
            {
                var connect = attempt.Connect;
                if (request.ConnectTimeout <= TimeSpan.Zero)
                {
                    return await CreateFacadeAsync(connect).ConfigureAwait(false);
                }

                var timeoutTask = Task.Delay(request.ConnectTimeout, _timeProvider,
                    _shutdown.Token);
                if (await Task.WhenAny(connect, timeoutTask).ConfigureAwait(false) == connect)
                {
                    return await CreateFacadeAsync(connect).ConfigureAwait(false);
                }

                await attempt.CancelAsync().ConfigureAwait(false);
                _ = attempt.DisposeWhenCompleteAsync();
                transferredOwnership = true;
                throw new TimeoutException("Connecting to the managed OPC UA session timed out.");
            }
            finally
            {
                if (!transferredOwnership)
                {
                    attempt.Dispose();
                }
            }
        }

        private async Task<ManagedOpcUaSession> CreateFacadeAsync(
            Task<IManagedSessionConnection> connect)
        {
            var connection = await connect.ConfigureAwait(false);
            try
            {
                return new ManagedOpcUaSession(connection, _telemetry, _timeProvider,
                    _options.NodeCacheTimeout, _options.NodeCacheCapacity);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                try
                {
                    await connection.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception disposeException) when (
                    disposeException is not OutOfMemoryException)
                {
                }
                throw;
            }
        }

        private async Task CloseAfterLingerAsync(Entry entry, int generation)
        {
            try
            {
                if (_options.LingerTimeout > TimeSpan.Zero)
                {
                    await Task.Delay(_options.LingerTimeout, _timeProvider,
                        _shutdown.Token).ConfigureAwait(false);
                }
                if (!await entry.TryStartClosingAsync(generation,
                    _shutdown.Token).ConfigureAwait(false))
                {
                    return;
                }
                if (Remove(entry))
                {
                    await entry.CloseAsync().ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Pool disposal closes entries explicitly.
            }
        }

        private bool Remove(Entry entry)
        {
            return ((ICollection<KeyValuePair<ConnectionIdentifier, Entry>>)_sessions)
                .Remove(new KeyValuePair<ConnectionIdentifier, Entry>(entry.Key, entry));
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        }

        private sealed class Entry : IDisposable
        {
            public Entry(ManagedSessionPool owner, ConnectionIdentifier key,
                ManagedSessionConnectionRequest request)
            {
                _owner = owner;
                Key = key;
                _request = request;
            }

            public ConnectionIdentifier Key { get; }

            public async Task<ISessionHandle> AcquireAsync(CancellationToken ct)
            {
                Task<ManagedOpcUaSession> connect;
                await _gate.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    if (_closing)
                    {
                        throw new EntryClosingException();
                    }
                    Interlocked.Increment(ref _generation);
                    _connect ??= _owner.ConnectAsync(_request);
                    connect = _connect;
                }
                finally
                {
                    _gate.Release();
                }

                var session = await connect.WaitAsync(ct).ConfigureAwait(false);
                await _gate.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    if (_closing)
                    {
                        throw new EntryClosingException();
                    }
                    _references++;
                    return new ManagedSessionLease(session,
                        _owner._options.ServiceCallTimeout, Release);
                }
                finally
                {
                    _gate.Release();
                }
            }

            public void Release()
            {
                if (Interlocked.Decrement(ref _references) != 0)
                {
                    return;
                }
                var generation = Interlocked.Increment(ref _generation);
                _ = _owner.CloseAfterLingerAsync(this, generation);
            }

            public async Task<bool> TryStartClosingAsync(int generation,
                CancellationToken ct)
            {
                await _gate.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    if (_closing || _references != 0 ||
                        Volatile.Read(ref _generation) != generation)
                    {
                        return false;
                    }
                    _closing = true;
                    return true;
                }
                finally
                {
                    _gate.Release();
                }
            }

            public async Task CloseAsync()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0)
                {
                    return;
                }

                Task<ManagedOpcUaSession>? connect;
                await _gate.WaitAsync().ConfigureAwait(false);
                try
                {
                    _closing = true;
                    connect = _connect;
                }
                finally
                {
                    _gate.Release();
                }

                if (connect == null)
                {
                    Dispose();
                    return;
                }
                try
                {
                    var session = await connect.ConfigureAwait(false);
                    await session.DisposeAsync().ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    Dispose();
                }
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _gateDisposed, 1) != 0)
                {
                    return;
                }
                _gate.Dispose();
            }

            private int _disposed;
            private int _gateDisposed;
            private int _generation;
            private int _references;
            private bool _closing;
            private Task<ManagedOpcUaSession>? _connect;
            private readonly ManagedSessionPool _owner;
            private readonly ManagedSessionConnectionRequest _request;
            private readonly SemaphoreSlim _gate = new(1, 1);
        }

        private sealed class ConnectionAttempt : IDisposable
        {
            public ConnectionAttempt(IManagedSessionProvider provider,
                ManagedSessionConnectionRequest request, CancellationToken shutdown)
            {
                _cancellation = CancellationTokenSource.CreateLinkedTokenSource(shutdown);
                try
                {
                    Connect = provider.ConnectAsync(request, _cancellation.Token);
                }
                catch
                {
                    _cancellation.Dispose();
                    throw;
                }
            }

            public Task<IManagedSessionConnection> Connect { get; }

            public Task CancelAsync()
            {
                return _cancellation.CancelAsync();
            }

            public async Task DisposeWhenCompleteAsync()
            {
                try
                {
                    var connection = await Connect.ConfigureAwait(false);
                    await connection.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OutOfMemoryException)
                {
                }
                finally
                {
                    Dispose();
                }
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 0)
                {
                    _cancellation.Dispose();
                }
            }

            private int _disposed;
            private readonly CancellationTokenSource _cancellation;
        }

        private sealed class ManagedSessionLease : ISessionHandle
        {
            public ManagedSessionLease(ManagedOpcUaSession session,
                TimeSpan serviceCallTimeout, Action release)
            {
                Session = session;
                ServiceCallTimeout = serviceCallTimeout;
                _release = release;
            }

            /// <inheritdoc/>
            public IOpcUaSession Session { get; }

            /// <inheritdoc/>
            public TimeSpan ServiceCallTimeout { get; }

            /// <inheritdoc/>
            public void Dispose()
            {
                Interlocked.Exchange(ref _release, null)?.Invoke();
            }

            private Action? _release;
        }

        private sealed class EntryClosingException : InvalidOperationException
        {
        }

        private int _disposed;
        private readonly IManagedSessionProvider _provider;
        private readonly ITelemetryContext _telemetry;
        private readonly ManagedSessionPoolOptions _options;
        private readonly TimeProvider _timeProvider;
        private readonly CancellationTokenSource _shutdown = new();
        private readonly ConcurrentDictionary<ConnectionIdentifier, Entry> _sessions = new();
    }
}
