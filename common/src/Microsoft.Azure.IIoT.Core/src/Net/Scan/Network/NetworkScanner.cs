// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Scanner {
    using Microsoft.Azure.IIoT.Net.Models;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.NetworkInformation;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Scans network using icmp and finds all machines in it.
    /// </summary>
    public sealed class NetworkScanner : IScanner {

        /// <summary>
        /// Number of items scanned
        /// </summary>
        public int ScanCount => _scanCount;

        /// <summary>
        /// Number of active probes
        /// </summary>
        public int ActiveProbes => _pings.Count;

        /// <summary>
        /// Create scanner
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="replies"></param>
        /// <param name="ct"></param>
        public NetworkScanner(ILogger logger, Action<NetworkScanner, PingReply> replies,
            CancellationToken ct) :
            this(logger, replies, false, null, NetworkClass.Wired, null, null, ct) {
        }

        /// <summary>
        /// Create scanner
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="replies"></param>
        /// <param name="netclass"></param>
        /// <param name="ct"></param>
        public NetworkScanner(ILogger logger, Action<NetworkScanner, PingReply> replies,
            NetworkClass netclass, CancellationToken ct) :
            this(logger, replies, false, null, netclass, null, null, ct) {
        }

        /// <summary>
        /// Create scanner
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="replies"></param>
        /// <param name="local"></param>
        /// <param name="netclass"></param>
        /// <param name="ct"></param>
        public NetworkScanner(ILogger logger, Action<NetworkScanner, PingReply> replies,
            bool local, NetworkClass netclass, CancellationToken ct) :
            this(logger, replies, local, null, netclass, null, null, ct) {
        }

        /// <summary>
        /// Create scanner
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="replies"></param>
        /// <param name="addresses"></param>
        /// <param name="ct"></param>
        public NetworkScanner(ILogger logger, Action<NetworkScanner, PingReply> replies,
            IEnumerable<AddressRange> addresses, CancellationToken ct) :
            this(logger, replies, false,
                addresses ?? throw new ArgumentNullException(nameof(addresses)),
                NetworkClass.None, null, null, ct) {
        }

        /// <summary>
        /// Create scanner
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="replies"></param>
        /// <param name="local"></param>
        /// <param name="addresses"></param>
        /// <param name="netclass"></param>
        /// <param name="maxProbeCount"></param>
        /// <param name="timeout"></param>
        /// <param name="ct"></param>
        public NetworkScanner(ILogger logger, Action<NetworkScanner, PingReply> replies,
            bool local, IEnumerable<AddressRange> addresses, NetworkClass netclass,
            int? maxProbeCount, TimeSpan? timeout, CancellationToken ct) {
            _logger = logger;
            _replies = replies;
            _ct = ct;
            _timeout = timeout ?? kDefaultProbeTimeout;
            _completion = new TaskCompletionSource<bool>();
            _candidates = new List<uint>();
            if (addresses == null) {
                _addresses = NetworkInformationEx.GetAllNetInterfaces(netclass)
                    .Select(t => new AddressRange(t, local)).Distinct().ToList();
            }
            else {
                _addresses = addresses.Select(a => a.Copy()).Distinct().ToList();
            }
            _pings = CreatePings(local ? _addresses.Count + 1 :
                maxProbeCount ?? kDefaultMaxProbeCount);
            // Start initial pings
            _logger.Information("Start scanning {addresses}...",
                _addresses.Select(a => a.ToString()));
            foreach (var ping in _pings.ToList()) {
                OnNextPing(ping);
            }
        }

        /// <summary>
        /// Scan completed
        /// </summary>
        public Task Completion => _completion.Task;

        /// <summary>
        /// Dispose scanner
        /// </summary>
        public void Dispose() {
            lock (_candidates) {
                foreach (var ping in _pings) {
                    ping.SendAsyncCancel();
                    ping.Dispose();
                }
                _pings.Clear();
                _completion.TrySetCanceled();
            }
        }

        /// <summary>
        /// Schedule next
        /// </summary>
        /// <param name="ping"></param>
        private void OnNextPing(Ping ping) {
            lock (_candidates) {
                while (!_ct.IsCancellationRequested) {
                    if (_candidates.Any()) {
                        var address = (IPv4Address)_candidates.First();
                        _candidates.RemoveAt(0);
                        ping.SendAsync(address, (int)_timeout.TotalMilliseconds,
                            ping);
                        return;
                    }

                    if (!_addresses.Any()) {
                        break;
                    }
                    _addresses.First().FillNextBatch(_candidates, kDefaultBatchSize);
                    if (!_candidates.Any()) {
                        _addresses.RemoveAt(0);
                        continue;
                    }
                    _candidates.Shuffle();
                }

                if (_pings.Remove(ping)) {
                    ping.Dispose();
                    if (!_pings.Any()) {
                        // All pings drained...
                        _completion.TrySetResult(true);
                    }
                }
            }
        }

        /// <summary>
        /// Helper to create ping
        /// </summary>
        /// <returns></returns>
        private List<Ping> CreatePings(int count) {
            var pings = new List<Ping>(count);
            for (var i = 0; i < count; i++) {
                var ping = new Ping();
                ping.PingCompleted += (sender, e) => {
                    var reply = e.Reply;
                    if (reply != null && reply.Status == IPStatus.Success) {
                        _replies(this, reply);
                    }
                    // When completed, grab next
                    Interlocked.Increment(ref _scanCount);
                    OnNextPing((Ping)e.UserState);
                };
                pings.Add(ping);
            }
            return pings;
        }

        private const int kDefaultMaxProbeCount = 100;
        private const int kDefaultBatchSize = 1000;
        private static readonly TimeSpan kDefaultProbeTimeout =
            TimeSpan.FromSeconds(3);

        private readonly TimeSpan _timeout;
        private readonly List<uint> _candidates;
        private readonly List<AddressRange> _addresses;
        private readonly ILogger _logger;
        private readonly List<Ping> _pings;
        private readonly Action<NetworkScanner, PingReply> _replies;
        private readonly TaskCompletionSource<bool> _completion;
        private readonly CancellationToken _ct;
        private volatile int _scanCount;
    }
}
