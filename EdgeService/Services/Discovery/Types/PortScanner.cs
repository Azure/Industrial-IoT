// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.EdgeService.Discovery {
    using Microsoft.Azure.IoTSolutions.Common.Diagnostics;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;

    /// <summary>
    /// Scans port ranges
    /// </summary>
    public class PortScanner : IDisposable {

        /// <summary>
        /// Timeout of a connect attempt in probe.  This has no bearing on
        /// Windows.  However, on Linux it allows timing out in lieu of epoll.
        /// </summary>
        public static TimeSpan ProbeTimeout { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Max number of connect probes. This corresponds to the machine's max
        /// number of user ports, e.g. on Windows this is set to 5000 by default.
        /// It is possible to increase this number in the registry:
        ///
        /// HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters
        ///     MaxUserPort: DWORD (5000-65534)
        ///
        /// Up to this number the performance improvement is linear, e.g. all
        /// ports on a Windows PC are scanned in around 16 seconds.  If the value
        /// is increased scanning is potentially faster.
        /// We set this to 4000 so that there is room for other connections.
        /// </summary>
        public static int MaxProbeCount { get; set; } = 5000;

        /// <summary>
        /// Create scanner
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="target"></param>
        /// <param name="ct"></param>
        public PortScanner(ILogger logger, ISourceBlock<IEnumerable<IPEndPoint>> source,
            ITargetBlock<IPEndPoint> target, CancellationToken ct) {
            _logger = logger;
            _source = source;
            _target = target;
            _candidates = new BlockingCollection<IPEndPoint>(MaxProbeCount * 10);
            _requeued = new ConcurrentQueue<IPEndPoint>();
            _rand = new Random();

            _probes = EnumerableEx
                .Repeat(i => new Probe(this, i), MaxProbeCount)
                .ToList();

            _cts = new CancellationTokenSource();
            _producer = Task.Factory.StartNew(ProduceAsync, _cts.Token,
                TaskCreationOptions.DenyChildAttach | TaskCreationOptions.LongRunning,
                TaskScheduler.Current);
            _active = MaxProbeCount;
            foreach (var probe in _probes) {
                probe.Start();
            }
            ct.Register(_cts.Cancel);
        }

        /// <summary>
        /// Scan completed
        /// </summary>
        public Task Completion => _target.Completion;

        /// <summary>
        /// Scan range of addresses and return the ones that are open
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="range"></param>
        /// <param name="ct"></param>
        public static async Task<IEnumerable<IPEndPoint>> ScanAsync(ILogger logger,
            IEnumerable<IPEndPoint> range, CancellationToken ct) {
            var result = new List<IPEndPoint>();
            var input = new BufferBlock<IEnumerable<IPEndPoint>>();
            input.Post(range);
            input.Complete();
            var output = new ActionBlock<IPEndPoint>(ep => {
                result.Add(ep);
#if TRACE
                logger.Debug($"{ep} open.", () => { });
#endif
            });
            using (var scanner = new PortScanner(logger, input, output, ct)) {
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
            if (!_producer.IsCompleted) {
                _producer.Wait();
            }
            _active = 0;
            _target.Complete();
            // Clean up all probes
            foreach (var probe in _probes) {
                probe.Dispose();
            }
            _probes.Clear();
        }

        /// <summary>
        /// Producer producing endpoints into the blocking collection
        /// </summary>
        private async Task ProduceAsync() {
            while (!_cts.IsCancellationRequested && !_source.Completion.IsCompleted) {
                try {
                    var more = await _source.ReceiveAsync(_cts.Token);
                    foreach (var item in more) {
                        _candidates.Add(item);
                    }
                }
                catch (TaskCanceledException) { }
                catch (InvalidOperationException) {
                    // Source completed
                    break;
                }
                catch (Exception ex) {
                    _logger.Debug($"Error filling consumer queue", () => ex);
                }
            }
            _candidates.CompleteAdding();
        }

        /// <summary>
        /// Exit probe and if last propagate target complete
        /// </summary>
        private void OnProbeExit() {
            if (0 == Interlocked.Decrement(ref _active)) {
                // All probes drained - propagate target complete...
                _target.Complete();
            }
        }

        /// <summary>
        /// Returns probe timeout in milliseconds but with entropy
        /// </summary>
        private int ProbeTimeoutInMilliseconds => _rand.Next(
            (int)(ProbeTimeout.TotalMilliseconds * 0.7),
            (int)(ProbeTimeout.TotalMilliseconds * 1.5));

        /// <summary>
        /// A probe is a ip endpoint consumer.
        /// </summary>
        /// <returns></returns>
        private class Probe : IDisposable {

            /// <summary>
            /// Create probe
            /// </summary>
            /// <param name="scanner"></param>
            /// <param name="index"></param>
            public Probe(PortScanner scanner, int index) {
                _index = index;
                _scanner = scanner;
                _timer = new Timer(OnTimeout);
                _lock = new SemaphoreSlim(1);
                _arg = new SocketAsyncEventArgs();
                _arg.Completed += OnCompleteConnect;
            }

            /// <summary>
            /// Dispose probe
            /// </summary>
            public void Dispose() {
                if (_disposed) {
                    return;
                }
                _lock.Wait();
                try {
                    if (!_disposed) {
                        _disposed = true;
                        Socket.CancelConnectAsync(_arg);
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
                if (!_disposed) {
#if FALSE
                    Task.Delay(_scanner._rand.Next(0, 500))
                        .ContinueWith(_ => OnBeginConnectAsync(_arg));
#else
                    Task.Run(() => OnBeginConnectAsync(_arg));
#endif
                }
            }

            /// <summary>
            /// Complete and begin next
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="arg"></param>
            private void OnCompleteConnect(object sender, SocketAsyncEventArgs arg) {
                _lock.Wait();
                try {
                    // Cancel timer
                    _timer.Change(Timeout.Infinite, Timeout.Infinite);
                    CompleteConnectNoLock(arg);
                }
                catch (Exception ex) {
                    _scanner._logger.Debug($"Error during completion of probe {_index}",
                        () => ex);
                }
                finally {
                    _lock.Release();
                }
                // Start next connect
                OnBeginConnectAsync(arg);
            }

            /// <summary>
            /// Start connect
            /// </summary>
            /// <param name="arg"></param>
            private async void OnBeginConnectAsync(SocketAsyncEventArgs arg) {
                await _lock.WaitAsync();
                try {
                    if (_disposed) {
                        return;
                    }

                    var exit = false;
                    while (!_disposed) {
                        IPEndPoint ep = null;
                        try {
                            if (!_scanner._candidates.TryTake(out ep, -1, _scanner._cts.Token)) {
                                if (!_scanner._requeued.TryDequeue(out ep)) {
                                    break;
                                }
                            }
                        }
                        catch (OperationCanceledException) {
                            break;
                        }
                        catch (InvalidOperationException) {
                            break;
                        }
                        catch (Exception ex) {
                            _scanner._logger.Error($"Error getting endpoint for probe {_index}",
                                () => ex);
                            break;
                        }

                        // Now try to connect
                        arg.RemoteEndPoint = ep;
                        while (!_disposed) {
                            try {
                                if (!Socket.ConnectAsync(SocketType.Stream, ProtocolType.IP,
                                    arg)) {
                                    // Complete inline and pull next...
                                    CompleteConnectNoLock(arg);
                                    break;
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
                                if (sex.SocketErrorCode == SocketError.NoBufferSpaceAvailable) {
                                    // Ran out of user ports, close and requeue...
                                    _scanner._logger.Debug(
                                        $"No more ports - dispose probe {_index}...",
                                        () => { });
                                    //exit = true;
                                    //break;
                                    continue;
                                }
                                _scanner._logger.Error(
                                    $"{sex.SocketErrorCode} in connect of probe {_index}...",
                                        () => sex);
                                // Try again
                                continue;
                            }
                            catch (Exception ex) {
                                // Unexpected
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
                    _arg.Dispose();
                    _disposed = true;
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
            private void CompleteConnectNoLock(SocketAsyncEventArgs arg) {
                if (arg.SocketError == SocketError.Success) {
                    _scanner._target.SendAsync((IPEndPoint)arg.RemoteEndPoint).Wait();
                }
                if (arg.ConnectSocket != null) {
                    arg.ConnectSocket.Close(0);
                    arg.ConnectSocket.Dispose();
                }
                arg.SocketError = SocketError.NotSocket;
            }


            /// <summary>
            /// Complete and begin next
            /// </summary>
            /// <param name="state"></param>
            private void OnTimeout(object state) {
                var takeLock = _lock.WaitAsync(0);
                try {
                    if (!takeLock.IsCompletedSuccessfully) {
                        // lock is taken
                        return;
                    }
                    // Cancel current arg and mark as timedout then recycle
                    Socket.CancelConnectAsync(_arg);
                    _arg.SocketError = SocketError.TimedOut;
                }
                catch (Exception ex) {
                    _scanner._logger.Debug($"Error during timeout of probe {_index}",
                        () => ex);
                }
                _lock.Release();
            }

            private readonly PortScanner _scanner;
            private readonly SocketAsyncEventArgs _arg;
            private readonly SemaphoreSlim _lock;
            private readonly int _index;
            private readonly Timer _timer;
            private bool _disposed;
        }

        private readonly BlockingCollection<IPEndPoint> _candidates;
        private readonly ConcurrentQueue<IPEndPoint> _requeued;
        private readonly ILogger _logger;
        private readonly List<Probe> _probes;
        private readonly ITargetBlock<IPEndPoint> _target;
        private readonly ISourceBlock<IEnumerable<IPEndPoint>> _source;
        private readonly CancellationTokenSource _cts;
        private readonly Task _producer;
        private readonly Random _rand;
        private int _active;
    }
}
