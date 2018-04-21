// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcTwin.EdgeService.Discovery {
    using Microsoft.Azure.IIoT.Common.Diagnostics;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Scans port ranges
    /// </summary>
    public class PortScanner : IDisposable {

        /// <summary>
        /// Number of items scanned
        /// </summary>
        public int ScanCount => _scanCount;

        /// <summary>
        /// Number of active probes
        /// </summary>
        public int ActiveProbes => _active;

        /// <summary>
        /// Create scanner with default port probe
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="ct"></param>
        public PortScanner(ILogger logger, IEnumerable<IPEndPoint> source,
            Action<IPEndPoint> target, CancellationToken ct) :
            this(logger, source, target, null, ct) {
        }

        /// <summary>
        /// Create scanner
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="portProbe"></param>
        /// <param name="ct"></param>
        public PortScanner(ILogger logger, IEnumerable<IPEndPoint> source,
            Action<IPEndPoint> target, IPortProbe portProbe, CancellationToken ct) :
            this(logger, source, target, portProbe, null, null, ct) {
        }

        /// <summary>
        /// Create scanner
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="portProbe"></param>
        /// <param name="ct"></param>
        public PortScanner(ILogger logger, IEnumerable<IPEndPoint> source,
            Action<IPEndPoint> target, IPortProbe portProbe,
            int? maxProbeCount, TimeSpan? timeout, CancellationToken ct) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _source = source?.GetEnumerator() ??
                throw new ArgumentNullException(nameof(source));
            _target = target ?? throw new ArgumentNullException(nameof(target));

            _maxProbeCount = maxProbeCount ?? kDefaultMaxProbeCount;
            _minProbeCount = _maxProbeCount / 10;
            _timeout = timeout ?? kDefaultProbeTimeout;
            _portProbe = portProbe ?? new NullPortProbe();
            _requeued = new ConcurrentQueue<IPEndPoint>();
            _rand = new Random();

            _probePool = EnumerableEx
                .Repeat(i => new ConnectProbe(this, i), _maxProbeCount)
                .ToList();

            _cts = new CancellationTokenSource();
            ct.Register(_cts.Cancel);
            _completion = new TaskCompletionSource<bool>();
            _active = _maxProbeCount;
            foreach (var probe in _probePool) {
                probe.Start();
            }
        }

        /// <summary>
        /// Scan completed
        /// </summary>
        public Task Completion => _completion.Task;

        /// <summary>
        /// Scan range of addresses and return the ones that are open
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="range"></param>
        /// <param name="probe"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<IPEndPoint>> ScanAsync(ILogger logger,
            IEnumerable<IPEndPoint> range, IPortProbe probe, CancellationToken ct) {
            var result = new List<IPEndPoint>();
            using (var scanner = new PortScanner(logger, range, ep => {
                result.Add(ep);
#if TRACE
                logger.Debug($"{ep} open.", () => { });
#endif
            }, probe, ct)) {
                await scanner.Completion;
            }
            return result;
        }

        /// <summary>
        /// Dispose scanner
        /// </summary>
        public void Dispose() {
            // Kill producer
            _cts.Cancel();
            // Clean up all probes
            _active = 0;
            foreach (var probe in _probePool) {
                probe.Dispose();
            }
            _probePool.Clear();
            _completion.TrySetCanceled();
        }

        /// <summary>
        /// Return next from source.
        /// </summary>
        private bool Next(out IPEndPoint ep) {
            if (_cts.IsCancellationRequested) {
                ep = null;
                return false;
            }
            lock (_source) {
                if (!_source.MoveNext()) {
                    ep = null;
                    return false;
                }
                ep = _source.Current;
            }
            return true;
        }

        /// <summary>
        /// Exit probe and if last propagate target complete
        /// </summary>
        private void OnProbeExit() {
            if (0 == Interlocked.Decrement(ref _active)) {
                // All probes drained - propagate target complete...
                if (_cts.IsCancellationRequested) {
                    _completion.TrySetCanceled();
                }
                else {
                    _completion.TrySetResult(true);
                }
            }
        }

        /// <summary>
        /// Returns probe timeout in milliseconds but with entropy
        /// </summary>
        private int ProbeTimeoutInMilliseconds => _rand.Next(
            (int)(_timeout.TotalMilliseconds * 0.7),
            (int)(_timeout.TotalMilliseconds * 1.5));

        /// <summary>
        /// Null port probe
        /// </summary>
        private class NullPortProbe : IAsyncProbe, IPortProbe {

            /// <inheritdoc />
            public bool CompleteAsync(int index, SocketAsyncEventArgs arg,
                out bool ok, out int timeout) {
                ok = true;
                timeout = 0;
                return true;
            }

            /// <inheritdoc />
            public void Dispose() {}

            /// <inheritdoc />
            public bool Reset() => false;

            /// <inheritdoc />
            public IAsyncProbe Create() => this;
        }

        /// <summary>
        /// A connect probe is a ip endpoint consumer that tries to connect
        /// to the consumed ip endpoint.  if successful it uses the probe
        /// implementation to interrogate the server.
        /// </summary>
        /// <returns></returns>
        private class ConnectProbe : IDisposable {

            /// <summary>
            /// Internal probe state
            /// </summary>
            private enum State {
                Begin,
                Connect,
                Probe,
                Timeout,
                Disposed
            }

            /// <summary>
            /// Create probe
            /// </summary>
            /// <param name="scanner"></param>
            /// <param name="index"></param>
            public ConnectProbe(PortScanner scanner, int index) {
                _index = index;
                _scanner = scanner;
                _timer = new Timer(OnTimeout);
                _lock = new SemaphoreSlim(1);
                _arg = new SocketAsyncEventArgs();
                _arg.Completed += OnComplete;
                _probe = scanner._portProbe.Create();
            }

            /// <summary>
            /// Dispose probe
            /// </summary>
            public void Dispose() {
                if (_state == State.Disposed) {
                    return;
                }
                _lock.Wait();
                try {
                    if (_state != State.Disposed) {
                        _state = State.Disposed;
                        Socket.CancelConnectAsync(_arg);
                        _probe.Dispose();
                        _arg.Dispose();
                    }
                }
                finally {
                    _lock.Release();
                }
            }

            /// <summary>
            /// Start probe
            /// </summary>
            internal void Start() {
                if (_state != State.Disposed) {
#if FALSE
                    Task.Delay(_scanner._rand.Next(0, 500)).ContinueWith(_ =>
                    Task.Run(() => OnBeginAsync(_arg)));
#else
                    Task.Run(() => OnBeginAsync(_arg));
#endif
                }
            }

            /// <summary>
            /// Complete
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="arg"></param>
            private void OnComplete(object sender, SocketAsyncEventArgs arg) {
                _lock.Wait();
                try {
                    if (!OnCompleteNoLock(arg)) {
                        return; // assume next OnComplete will occur or timeout...
                    }

                    // Cancel timer and start next
                    _timer.Change(Timeout.Infinite, Timeout.Infinite);
                }
                catch (Exception ex) {
                    _scanner._logger.Debug($"Error during completion of probe {_index}",
                        () => ex);
                }
                finally {
                    _lock.Release();
                }
                // We are now disposed, or at begin, go to next to cleanup or continue
                OnBeginAsync(arg);
            }

            /// <summary>
            /// Start connect
            /// </summary>
            /// <param name="arg"></param>
            private async void OnBeginAsync(SocketAsyncEventArgs arg) {
                await _lock.WaitAsync();
                try {
                    if (_state == State.Disposed) {
                        return;
                    }

                    var exit = false;
                    while (_state != State.Disposed && !exit ) {
                        IPEndPoint ep = null;
                        try {
                            if (!_scanner.Next(out ep)) {
                                exit = true;
                                break;
                            }
                            Interlocked.Increment(ref _scanner._scanCount);
                        }
                        catch (OperationCanceledException) {
                            exit = true;
                            break;
                        }
                        catch (InvalidOperationException) {
                            continue;
                        }
                        catch (Exception ex) {
                            _scanner._logger.Error($"Error getting endpoint for probe {_index}",
                                () => ex);
                            exit = true;
                            break;
                        }

                        // Now try to connect
                        arg.RemoteEndPoint = ep;
                        while (_state != State.Disposed) {
                            try {
                                // Reset probe and start connect
                                _probe.Reset();
                                _state = State.Connect;
                                if (!Socket.ConnectAsync(SocketType.Stream, ProtocolType.IP, arg)) {
                                    // Complete inline and pull next...
                                    if (OnCompleteNoLock(arg)) {
                                        // Go to next candidate
                                        break;
                                    }
                                }
                                // Wait for completion or timeout after x seconds
                                _timer.Change(_scanner.ProbeTimeoutInMilliseconds, Timeout.Infinite);
                                return;
                            }
                            catch (ObjectDisposedException) {
                                continue; // Try again
                            }
                            catch (InvalidOperationException) {
                                continue; // Operation in progress - try again
                            }
                            catch (SocketException sex) {
                                if (sex.SocketErrorCode == SocketError.NoBufferSpaceAvailable ||
                                    sex.SocketErrorCode == SocketError.TooManyOpenSockets) {
                                    if (_scanner._scanCount > _scanner._minProbeCount) {
                                        // Exit only until we hit min probe count.
                                        exit = true;
                                        break;
                                    }
                                    // Otherwise retry...
                                }
                                _scanner._logger.Error(
                                    $"{sex.SocketErrorCode} in connect of probe {_index}...",
                                        () => sex);
                                continue;
                            }
                            catch (Exception ex) {
                                // Unexpected - shut probe down
                                _scanner._logger.Error(
                                    $"Probe {_index} has unexpected exception during connect.",
                                    () => ex);
                                exit = true;
                                break;
                            }
                        }

                        if (exit) {
                            // We failed, requeue the endpoint and kill this probe
                            _scanner._requeued.Enqueue(ep);
                            Interlocked.Decrement(ref _scanner._scanCount);
                            break;
                        }
                    }

                    //
                    // We are here because we either...
                    //
                    // a) failed to dequeue
                    // b) The producer has completed
                    // c) Connect failed due to lack of ephimeral ports.
                    // d) A non socket exception occurred.
                    //
                    // and we need to kill this probe and dispose of the underlying argument.
                    //
                    _probe.Dispose();
                    _arg.Dispose();
                    _state = State.Disposed;
                    _scanner.OnProbeExit();
                }
                finally {
                    _lock.Release();
                }
            }

            /// <summary>
            /// No lock complete
            /// </summary>
            /// <param name="arg"></param>
            private bool OnCompleteNoLock(SocketAsyncEventArgs arg) {
                try {
                    switch (_state) {
                        case State.Connect:
                            if (arg.SocketError != SocketError.Success) {
                                // Reset back to begin
                                _state = State.Begin;
                                return true;
                            }
                            // Start probe
                            _state = State.Probe;
                            return OnProbeNoLock(arg);
                        case State.Probe:
                            // Continue probing until completed
                            return OnProbeNoLock(arg);
                        case State.Timeout:
                            // Timeout caused cancel, go back to begin
                            _state = State.Begin;
                            return true;
                        case State.Disposed:
                            // Stay disposed
                            return true;
                        default:
                            throw new SystemException("Unexpected");
                    }
                }
                catch {
                    // Error, continue at beginning
                    _state = State.Begin;
                    return true;
                }
                finally {
                    if (_state == State.Begin || _state == State.Disposed) {
                        arg.ConnectSocket.SafeDispose();
                        arg.SocketError = SocketError.NotSocket;
                    }
                }
            }

            /// <summary>
            /// Perform probe
            /// </summary>
            /// <param name="arg"></param>
            /// <returns></returns>
            private bool OnProbeNoLock(SocketAsyncEventArgs arg) {
                var completed = _probe.CompleteAsync(_index, arg,
                    out var ok, out var timeout);
                if (completed) {
                    if (ok) {
                        // Back pressure...
                        _scanner._target((IPEndPoint)arg.RemoteEndPoint);
                    }
                    _state = State.Begin;
                }
                else {
                    // Wait for completion or timeout after x seconds
                    _timer.Change(timeout, Timeout.Infinite);
                }
                return completed;
            }

            /// <summary>
            /// Timeout current probe
            /// </summary>
            /// <param name="state"></param>
            private void OnTimeout(object state) {
                if (!_lock.Wait(0)) {
                    // lock is taken either by having completed or having re-begun.
                    return;
                }
                try {
                    switch (_state) {
                        case State.Timeout:
                            return;
                        case State.Connect:
                            // Cancel current arg and mark as timedout then recycle
                            Socket.CancelConnectAsync(_arg);
                            _arg.SocketError = SocketError.TimedOut;
                            return;
                        case State.Probe:
// #if LOG_VERBOSE
                            _scanner._logger.Debug(
                                $"Probe {_index} {_arg.RemoteEndPoint} timed out...",
                                    () => { });
// #endif
                            _arg.SocketError = SocketError.TimedOut;
                            if (_probe.Reset()) {
                                return;
                            }
                            // Has not cancelled the operation in progress.
                            _scanner._logger.Info(
                                $"Probe {_index} not cancelled - try restart...",
                                    () => { });
                            OnBeginAsync(_arg);
                            return;
                    }
                }
                catch (Exception ex) {
                    _scanner._logger.Debug($"Error during timeout of probe {_index}",
                        () => ex);
                }
                finally {
                    _state = State.Timeout;
                    _lock.Release();
                }
            }

            private readonly PortScanner _scanner;
            private readonly SocketAsyncEventArgs _arg;
            private readonly SemaphoreSlim _lock;
            private readonly int _index;
            private readonly Timer _timer;
            private readonly IAsyncProbe _probe;
            private State _state;
        }


        /// <summary>
        /// Max number of connect probes. On Windows, up to 5000 the performance
        /// improvement is linear, e.g. all ports on a Windows PC are scanned in
        /// around 16 seconds.
        /// </summary>
        private const int kDefaultMaxProbeCount = 4000;
        private readonly TimeSpan kDefaultProbeTimeout =
            TimeSpan.FromSeconds(10);

        private readonly ConcurrentQueue<IPEndPoint> _requeued;
        private readonly IEnumerator<IPEndPoint> _source;
        private readonly TaskCompletionSource<bool> _completion;
        private readonly List<ConnectProbe> _probePool;
        private readonly Action<IPEndPoint> _target;
        private readonly int _maxProbeCount;
        private readonly int _minProbeCount;
        private readonly TimeSpan _timeout;
        private readonly CancellationTokenSource _cts;
        private readonly Random _rand;
        private readonly IPortProbe _portProbe;
        private readonly ILogger _logger;
        private int _active;
        private int _scanCount;
    }
}
