// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net {
    using Microsoft.Azure.IIoT.Net.Models;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Scan services
    /// </summary>
    public interface IScanServices {

        /// <summary>
        /// Scan network
        /// </summary>
        /// <param name="replies"></param>
        /// <param name="local"></param>
        /// <param name="addresses"></param>
        /// <param name="netclass"></param>
        /// <param name="maxProbeCount"></param>
        /// <param name="timeout"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task ScanAsync(Action<PingReply> replies, bool local,
            IEnumerable<AddressRange> addresses, NetworkClass netclass,
            int? maxProbeCount, TimeSpan? timeout, CancellationToken ct);

        /// <summary>
        /// Scan ports
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="portProbe"></param>
        /// <param name="maxProbeCount"></param>
        /// <param name="minProbePercent"></param>
        /// <param name="timeout"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task ScanAsync(IEnumerable<IPEndPoint> source, Action<IPEndPoint> target,
            IPortProbe portProbe, int? maxProbeCount, int? minProbePercent,
            TimeSpan? timeout, CancellationToken ct);
    }
}