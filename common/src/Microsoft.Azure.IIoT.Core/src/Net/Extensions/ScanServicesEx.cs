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
    /// Scanner services
    /// </summary>
    public static class ScanServicesEx {

        /// <summary>
        /// Scan entire network
        /// </summary>
        /// <param name="services"></param>
        /// <param name="netclass"></param>
        /// <param name="ct"></param>
        public static async Task<IEnumerable<PingReply>> ScanAsync(
            this IScanServices services, NetworkClass netclass, CancellationToken ct) {
            var result = new List<PingReply>();
            await services.ScanAsync(reply => result.Add(reply), netclass, ct);
            return result;
        }

        /// <summary>
        /// Scan network
        /// </summary>
        /// <param name="services"></param>
        /// <param name="replies"></param>
        /// <param name="ct"></param>
        public static Task ScanAsync(this IScanServices services, Action<PingReply> replies,
            CancellationToken ct) {
            return services.ScanAsync(replies, false, null, NetworkClass.Wired, null, null, ct);
        }

        /// <summary>
        /// Create scanner
        /// </summary>
        /// <param name="services"></param>
        /// <param name="replies"></param>
        /// <param name="netclass"></param>
        /// <param name="ct"></param>
        public static Task ScanAsync(this IScanServices services, Action<PingReply> replies,
            NetworkClass netclass, CancellationToken ct) {
            return services.ScanAsync(replies, false, null, netclass, null, null, ct);
        }

        /// <summary>
        /// Create scanner
        /// </summary>
        /// <param name="services"></param>
        /// <param name="replies"></param>
        /// <param name="local"></param>
        /// <param name="netclass"></param>
        /// <param name="ct"></param>
        public static Task ScanAsync(this IScanServices services, Action<PingReply> replies,
            bool local, NetworkClass netclass, CancellationToken ct) {
            return services.ScanAsync(replies, local, null, netclass, null, null, ct);
        }

        /// <summary>
        /// Create scanner
        /// </summary>
        /// <param name="services"></param>
        /// <param name="replies"></param>
        /// <param name="addresses"></param>
        /// <param name="ct"></param>
        public static Task ScanAsync(this IScanServices services, Action<PingReply> replies,
            IEnumerable<AddressRange> addresses, CancellationToken ct) {
            return services.ScanAsync(replies, false, addresses ??
throw new ArgumentNullException(nameof(addresses)),
NetworkClass.None, null, null, ct);
        }

        /// <summary>
        /// Create scanner with default port probe
        /// </summary>
        /// <param name="services"></param>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="ct"></param>
        public static Task ScanAsync(this IScanServices services, IEnumerable<IPEndPoint> source,
            Action<IPEndPoint> target, CancellationToken ct) {
            return services.ScanAsync(source, target, null, ct);
        }

        /// <summary>
        /// Create scanner
        /// </summary>
        /// <param name="services"></param>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="portProbe"></param>
        /// <param name="ct"></param>
        public static Task ScanAsync(this IScanServices services, IEnumerable<IPEndPoint> source,
            Action<IPEndPoint> target, IPortProbe portProbe, CancellationToken ct) {
            return services.ScanAsync(source, target, portProbe, 5000, null, null, ct);
        }

        /// <summary>
        /// Scan range of addresses and return the ones that are open
        /// </summary>
        /// <param name="services"></param>
        /// <param name="range"></param>
        /// <param name="probe"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<IPEndPoint>> ScanAsync(this IScanServices services,
            IEnumerable<IPEndPoint> range, IPortProbe probe, CancellationToken ct) {
            var result = new List<IPEndPoint>();
            await ScanAsync(services, range, ep => result.Add(ep), probe, ct);
            return result;
        }

        /// <summary>
        /// Scan range of addresses and return the ones that are open
        /// </summary>
        /// <param name="services"></param>
        /// <param name="range"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<IPEndPoint>> ScanAsync(this IScanServices services,
            IEnumerable<IPEndPoint> range, CancellationToken ct) {
            var result = new List<IPEndPoint>();
            await ScanAsync(services, range, ep => result.Add(ep), null, ct);
            return result;
        }
    }
}
