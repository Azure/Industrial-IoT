// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.EdgeService.Discovery {
    using Microsoft.Azure.IoTSolutions.Common.Diagnostics;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;

    /// <summary>
    /// Scans network using icmp and finds all machines in it.
    /// </summary>
    public class NetworkScanner : IDisposable {

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
        public NetworkScanner(ILogger logger, ITargetBlock<PingReply> replies,
            CancellationToken ct) :
            this(logger, replies, false, null, NetworkClass.Wired, null, null, ct) {
        }

        /// <summary>
        /// Create scanner
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="replies"></param>
        /// <param name="ct"></param>
        public NetworkScanner(ILogger logger, ITargetBlock<PingReply> replies,
            NetworkClass netclass, CancellationToken ct) :
            this(logger, replies, false, null, netclass, null, null, ct) {
        }

        /// <summary>
        /// Create scanner
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="replies"></param>
        /// <param name="ct"></param>
        public NetworkScanner(ILogger logger, ITargetBlock<PingReply> replies,
            bool local, NetworkClass netclass, CancellationToken ct) :
            this(logger, replies, local, null, netclass, null, null, ct) {
        }

        /// <summary>
        /// Create scanner
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="replies"></param>
        /// <param name="ct"></param>
        public NetworkScanner(ILogger logger, ITargetBlock<PingReply> replies,
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
        /// <param name="ct"></param>
        public NetworkScanner(ILogger logger, ITargetBlock<PingReply> replies,
            bool local, IEnumerable<AddressRange> addresses, NetworkClass netclass,
            int? maxProbeCount, TimeSpan? timeout, CancellationToken ct) {
            _logger = logger;
            _replies = replies;
            _ct = ct;
            _timeout = timeout ?? kDefaultProbeTimeout;
            _candidates = new List<uint>();
            if (addresses == null) {
                addresses = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n =>
                        IsInClass(n.NetworkInterfaceType, netclass) &&
                        !n.Name.Contains("(DockerNAT)") &&
                        n.OperationalStatus == OperationalStatus.Up &&
                        n.GetIPProperties() != null)
                    .SelectMany(n => n.GetIPProperties().UnicastAddresses
                        .Select(x => Tuple.Create(n.Name, x)))
                    .Where(t =>
                        t.Item2.Address.AddressFamily == AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(t.Item2.Address))
                    .Select(t => new AddressRange(t.Item2, t.Item1, local))
                    .Distinct();
            }
            _addresses = addresses.ToList();
            _pings = CreatePings(local ? _addresses.Count + 1 :
                maxProbeCount ?? kDefaultMaxProbeCount);
            // Start initial pings
            _logger.Info("Start scanning...",
                () => _addresses.Select(a => a.ToString()));
            foreach (var ping in _pings.ToList()) {
                OnNextPing(ping);
            }
        }

        /// <summary>
        /// Scan completed
        /// </summary>
        public Task Completion => _replies.Completion;

        /// <summary>
        /// Scan entire network
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="ct"></param>
        public static async Task<IEnumerable<PingReply>> ScanAsync(ILogger logger,
            NetworkClass netclass, CancellationToken ct) {
            var result = new List<PingReply>();
            var output = new ActionBlock<PingReply>(reply => {
                result.Add(reply);
#if TRACE
                logger.Debug($"{reply.Address} found.", () => { });
#endif
            });
            using (var scanner = new NetworkScanner(logger, output, netclass, ct)) {
                await scanner.Completion;
            }
            return result;
        }

        /// <summary>
        /// Dispose scanner
        /// </summary>
        public void Dispose() {
            lock (_candidates) {
                _replies.Complete();
                foreach (var ping in _pings) {
                    ping.SendAsyncCancel();
                    ping.Dispose();
                }
                _pings.Clear();
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
                        var address = AddressRange.ToAddress(_candidates.First());
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
                        _replies.Complete();
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
                        _replies.SendAsync(reply).Wait();
                    }
                    // When completed, grab next
                    Interlocked.Increment(ref _scanCount);
                    OnNextPing((Ping)e.UserState);
                };
                pings.Add(ping);
            }
            return pings;
        }

        private static bool IsInClass(NetworkInterfaceType type,
            NetworkClass netclass) {
            switch(type) {
                case NetworkInterfaceType.Ethernet:
                case NetworkInterfaceType.Ethernet3Megabit:
                case NetworkInterfaceType.GigabitEthernet:
                case NetworkInterfaceType.FastEthernetT:
                case NetworkInterfaceType.FastEthernetFx:
                case NetworkInterfaceType.Slip:
                case NetworkInterfaceType.IPOverAtm:
                    return (netclass & NetworkClass.Wired) != 0;

                case NetworkInterfaceType.BasicIsdn:
                case NetworkInterfaceType.PrimaryIsdn:
                case NetworkInterfaceType.Isdn:
                case NetworkInterfaceType.GenericModem:
                case NetworkInterfaceType.AsymmetricDsl:
                case NetworkInterfaceType.SymmetricDsl:
                case NetworkInterfaceType.RateAdaptDsl:
                case NetworkInterfaceType.VeryHighSpeedDsl:
                case NetworkInterfaceType.MultiRateSymmetricDsl:
                case NetworkInterfaceType.Ppp:
                    return (netclass & NetworkClass.Modem) != 0;

                case NetworkInterfaceType.Wireless80211:
                case NetworkInterfaceType.Wman:
                case NetworkInterfaceType.Wwanpp:
                case NetworkInterfaceType.Wwanpp2:
                    return (netclass & NetworkClass.Wireless) != 0;
                case NetworkInterfaceType.Tunnel:
                    return (netclass & NetworkClass.Tunnel) != 0;

                case NetworkInterfaceType.TokenRing:
                case NetworkInterfaceType.HighPerformanceSerialBus:
                case NetworkInterfaceType.Fddi:
                case NetworkInterfaceType.Atm:
                case NetworkInterfaceType.Loopback:
                    return false;
            }
            return false;
        }

        private const int kDefaultMaxProbeCount = 100;
        private const int kDefaultBatchSize = 1000;
        private readonly TimeSpan kDefaultProbeTimeout =
            TimeSpan.FromSeconds(3);

        private readonly TimeSpan _timeout;
        private readonly List<uint> _candidates;
        private readonly List<AddressRange> _addresses;
        private readonly ILogger _logger;
        private readonly List<Ping> _pings;
        private readonly ITargetBlock<PingReply> _replies;
        private readonly CancellationToken _ct;
        private int _scanCount;
    }
}
