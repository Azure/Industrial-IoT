// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Scanner {
    using Microsoft.Azure.IIoT.Net.Models;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Scanner services
    /// </summary>
    public sealed class ScanServices : IScanServices {

        /// <summary>
        /// Create scanner
        /// </summary>
        /// <param name="logger"></param>
        public ScanServices(ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Create scanner
        /// </summary>
        /// <param name="replies"></param>
        /// <param name="local"></param>
        /// <param name="addresses"></param>
        /// <param name="netclass"></param>
        /// <param name="maxProbeCount"></param>
        /// <param name="timeout"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task ScanAsync(Action<PingReply> replies, bool local,
            IEnumerable<AddressRange> addresses, NetworkClass netclass,
            int? maxProbeCount, TimeSpan? timeout, CancellationToken ct) {
            using (var scanner = new NetworkScanner(_logger, (s, p) => replies(p),
                local, addresses, netclass, maxProbeCount, timeout, ct)) {
                await scanner.Completion;
            }
        }


        /// <summary>
        /// Create scanner
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="portProbe"></param>
        /// <param name="maxProbeCount"></param>
        /// <param name="minProbePercent"></param>
        /// <param name="timeout"></param>
        /// <param name="ct"></param>
        public async Task ScanAsync(IEnumerable<IPEndPoint> source,
            Action<IPEndPoint> target, IPortProbe portProbe, int? maxProbeCount,
            int? minProbePercent, TimeSpan? timeout, CancellationToken ct) {
            using (var scanner = new PortScanner(_logger, source, (s, p) => target(p), portProbe,
                maxProbeCount, minProbePercent, timeout, ct)) {
                await scanner.Completion;
            }
        }

        private readonly ILogger _logger;
    }
}
