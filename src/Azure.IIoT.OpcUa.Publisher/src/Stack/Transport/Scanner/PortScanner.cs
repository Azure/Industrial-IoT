// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Transport.Scanner
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Scans port ranges
    /// </summary>
    public sealed class PortScanner : IScanner
    {
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
        public PortScanner(ILogger<PortScanner> logger, IEnumerable<IPEndPoint> source,
            Action<PortScanner, IPEndPoint> target, CancellationToken ct) :
            this(logger, source, target, null, ct)
        {
        }

        /// <summary>
        /// Create scanner
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="portProbe"></param>
        /// <param name="ct"></param>
        public PortScanner(ILogger<PortScanner> logger, IEnumerable<IPEndPoint> source,
            Action<PortScanner, IPEndPoint> target, IPortProbe? portProbe, CancellationToken ct) :
            this(logger, source, target, portProbe, null, null, null, ct)
        {
        }

        /// <summary>
        /// Create scanner
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="portProbe"></param>
        /// <param name="maxProbeCount"></param>
        /// <param name="minProbePercent"></param>
        /// <param name="timeout"></param>
        /// <param name="ct"></param>
        public PortScanner(ILogger<PortScanner> logger, IEnumerable<IPEndPoint> source,
            Action<PortScanner, IPEndPoint> target, IPortProbe? portProbe, int? maxProbeCount,
            int? minProbePercent, TimeSpan? timeout, CancellationToken ct)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _source = source?.GetEnumerator() ??
                throw new ArgumentNullException(nameof(source));
            _target = target ?? throw new ArgumentNullException(nameof(target));

            _maxProbeCount = maxProbeCount ?? kDefaultMaxProbeCount;
            _minProbeCount = (int)(_maxProbeCount *
                ((minProbePercent ?? kDefaultMinProbePercent) / 100.0));
            _timeout = timeout ?? kDefaultProbeTimeout;
            _portProbe = portProbe ?? new NullPortProbe();
            _requeued = new ConcurrentQueue<IPEndPoint>();

            _probePool = Repeat(i => new ConnectProbe(this, i), _maxProbeCount)
                .ToList();

            _cts = new CancellationTokenSource();
            ct.Register(_cts.Cancel);
            _completion = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            _active = _maxProbeCount;
            foreach (var probe in _probePool)
            {
                probe.Start();
            }

            static IEnumerable<T> Repeat<T>(Func<int, T> factory, int count)
            {
                for (var i = 0; i < count; i++)
                {
                    yield return factory(i);
                }
            }
        }

        /// <summary>
        /// Scan completed
        /// </summary>
        public Task WaitToCompleteAsync()
        {
            return _completion.Task;
        }

        /// <summary>
        /// Dispose scanner
        /// </summary>
        public void Dispose()
        {
            // Kill producer
            _cts.Cancel();
            // Clean up all probes
            _active = 0;
            foreach (var probe in _probePool)
            {
                probe.Dispose();
            }
            _probePool.Clear();
            _completion.TrySetCanceled();
            _cts.Dispose();
        }

        /// <summary>
        /// Return next from source.
        /// </summary>
        /// <param name="ep"></param>
        private bool Next([NotNullWhen(true)] out IPEndPoint? ep)
        {
            if (_cts.IsCancellationRequested)
            {
                ep = null;
                return false;
            }
            lock (_source)
            {
                if (!_source.MoveNext())
                {
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
        private void OnProbeExit()
        {
            if (Interlocked.Decrement(ref _active) == 0)
            {
                // All probes drained - propagate target complete...
                if (_cts.IsCancellationRequested)
                {
                    _completion.TrySetCanceled();
                }
                else
                {
                    _completion.TrySetResult(true);
                }
            }
        }

        /// <summary>
        /// Returns probe timeout in milliseconds but with entropy
        /// </summary>
#pragma warning disable CA5394 // Do not use insecure randomness
        private int ProbeTimeoutInMilliseconds => Random.Shared.Next(
            (int)(_timeout.TotalMilliseconds * 0.7),
            (int)(_timeout.TotalMilliseconds * 1.5));
#pragma warning restore CA5394 // Do not use insecure randomness

        /// <summary>
        /// Port connect probe
        /// </summary>
        private sealed class ConnectProbe : BaseConnectProbe
        {
            /// <summary>
            /// Create probe
            /// </summary>
            /// <param name="scanner"></param>
            /// <param name="index"></param>
            public ConnectProbe(PortScanner scanner, int index) :
                base(index, scanner._portProbe.Create(), scanner._logger)
            {
                _scanner = scanner;
            }

            /// <summary>
            /// Get next endpoint
            /// </summary>
            /// <param name="ep"></param>
            /// <param name="timeout"></param>
            /// <returns></returns>
            protected override bool GetNext(
                [NotNullWhen(true)] out IPEndPoint? ep, out int timeout)
            {
                if (!_scanner.Next(out ep))
                {
                    timeout = 0;
                    return false;
                }
                timeout = _scanner.ProbeTimeoutInMilliseconds;
                Interlocked.Increment(ref _scanner._scanCount);
                return true;
            }

            /// <summary>
            /// Called on error
            /// </summary>
            /// <returns></returns>
            protected override bool ShouldGiveUp()
            {
                return _scanner._active > _scanner._minProbeCount;
            }

            /// <summary>
            /// Called when endpoint probe failed for some reason
            /// </summary>
            /// <param name="ep"></param>
            protected override void OnFail(IPEndPoint ep)
            {
                _scanner._requeued.Enqueue(ep);
                Interlocked.Decrement(ref _scanner._scanCount);
            }

            /// <summary>
            /// Called on success
            /// </summary>
            /// <param name="ep"></param>
            protected override void OnSuccess(IPEndPoint ep)
            {
                _scanner._target(_scanner, ep);
            }

            /// <summary>
            /// Called when probe terminates
            /// </summary>
            protected override void OnExit()
            {
                _scanner.OnProbeExit();
            }

            private readonly PortScanner _scanner;
        }

        /// <summary>
        /// Null port probe
        /// </summary>
        private class NullPortProbe : IAsyncProbe, IPortProbe
        {
            /// <inheritdoc />
            public bool OnComplete(int index, SocketAsyncEventArgs arg,
                out bool ok, out int timeout)
            {
                ok = true;
                timeout = 0;
                return true;
            }

            /// <inheritdoc />
            public void Dispose() { }

            /// <inheritdoc />
            public bool Reset()
            {
                return false;
            }

            /// <inheritdoc />
            public IAsyncProbe Create()
            {
                return this;
            }
        }

        /// <summary>
        /// Max number of connect probes. On Windows, up to 5000 the performance
        /// improvement is linear, e.g. all ports on a Windows PC are scanned in
        /// around 16 seconds.
        /// </summary>
        private const int kDefaultMaxProbeCount = 1000;

        /// <summary>
        /// By default ensure at least 80% probes are going.
        /// </summary>
        private const int kDefaultMinProbePercent = 80;
        private static readonly TimeSpan kDefaultProbeTimeout =
            TimeSpan.FromSeconds(5);

        private readonly ConcurrentQueue<IPEndPoint> _requeued;
        private readonly IEnumerator<IPEndPoint> _source;
        private readonly TaskCompletionSource<bool> _completion;
        private readonly List<ConnectProbe> _probePool;
        private readonly Action<PortScanner, IPEndPoint> _target;
        private readonly int _maxProbeCount;
        private readonly int _minProbeCount;
        private readonly TimeSpan _timeout;
        private readonly CancellationTokenSource _cts;
        private readonly IPortProbe _portProbe;
        private readonly ILogger _logger;
        private int _active;
#pragma warning disable IDE0032 // Use auto property
        private int _scanCount;
#pragma warning restore IDE0032 // Use auto property
    }
}
