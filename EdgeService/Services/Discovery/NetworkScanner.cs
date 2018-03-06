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
        /// Create scanner
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="replies"></param>
        /// <param name="timeout"></param>
        /// <param name="ct"></param>
        public NetworkScanner(ILogger logger, Action<PingReply> replies,
            int timeout, CancellationToken ct) :
            this(logger, new ActionBlock<PingReply>(replies), timeout, ct) {
        }

        /// <summary>
        /// Create scanner
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="replies"></param>
        /// <param name="timeout"></param>
        /// <param name="ct"></param>
        public NetworkScanner(ILogger logger, ITargetBlock<PingReply> replies,
            int timeout, CancellationToken ct) {
            _logger = logger;
            _replies = replies;
            _timeout = timeout;
            _ct = ct;
            _candidates = new List<uint>();
            _addresses = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n =>
                    !n.IsReceiveOnly &&
                    n.OperationalStatus == OperationalStatus.Up &&
                    n.GetIPProperties() != null)
                .SelectMany(s => s.GetIPProperties().UnicastAddresses)
                .Where(x =>
                    x.Address.AddressFamily == AddressFamily.InterNetwork)
                .Where(x => !IPAddress.IsLoopback(x.Address))
                .Select(x => new AddressRange(x))
                .Distinct()
                .ToList();
            _pings = CreatePings(1000); // Starts scan

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
                        ping.SendAsync(address, _timeout, ping);
                        return;
                    }

                    if (!_addresses.Any()) {
                        break;
                    }
                    _addresses.First().FillNextBatch(_candidates, 1000);
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
                        _replies.Post(reply);
                    }
                    // When completed, grab next
                    OnNextPing((Ping)e.UserState);
                };
                pings.Add(ping);
            }
            return pings;
        }

        class AddressRange {
            public AddressRange(UnicastIPAddressInformation address) {
                var curAddr = (uint)IPAddress.NetworkToHostOrder(
                    (int)BitConverter.ToUInt32(
                    address.Address.GetAddressBytes(), 0));
                var mask = (uint)IPAddress.NetworkToHostOrder(
                    (int)BitConverter.ToUInt32(
                    address.IPv4Mask.GetAddressBytes(), 0));

                High = curAddr | ~mask;
                Low = _cur = (curAddr & mask);

                System.Diagnostics.Debug.Assert(Low <= High);
                System.Diagnostics.Debug.Assert(Low != 0);
                System.Diagnostics.Debug.Assert(High != 0);
            }

            public uint Low { get; }
            public uint High { get; }

            public override bool Equals(object obj) {
                if (!(obj is AddressRange range)) {
                    return false;
                }
                return Low == range.Low && High == range.High;
            }

            public override int GetHashCode() {
                var hashCode = 2082053542;
                hashCode = hashCode * -1521134295 + Low.GetHashCode();
                hashCode = hashCode * -1521134295 + High.GetHashCode();
                return hashCode;
            }

            public override string ToString() =>
                $"{ToAddress(Low)}-{ToAddress(High)}";

            public static bool operator ==(AddressRange range1,
                AddressRange range2) => range1.Equals(range2);

            public static bool operator !=(AddressRange range1,
                AddressRange range2) => !(range1 == range2);

            public static IPAddress ToAddress(uint addr) =>
                new IPAddress((uint)IPAddress.HostToNetworkOrder((int)addr));

            public void FillNextBatch(IList<uint> batch, int count) {
                for (var i = 0; _cur <= High && i < count; i++) {
                    batch.Add(_cur++);
                }
            }

            private uint _cur;
        }

        private readonly List<uint> _candidates;
        private readonly List<AddressRange> _addresses;
        private readonly ILogger _logger;
        private readonly List<Ping> _pings;
        private readonly ITargetBlock<PingReply> _replies;
        private readonly int _timeout;
        private readonly CancellationToken _ct;
    }
}
