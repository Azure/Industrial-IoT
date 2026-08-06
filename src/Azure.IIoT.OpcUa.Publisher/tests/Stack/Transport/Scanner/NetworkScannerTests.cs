// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack.Transport.Scanner
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Transport.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Transport.Scanner;
    using Microsoft.Extensions.Logging;
    using Moq;
    using System;
    using System.Net.NetworkInformation;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Unit tests for <see cref="NetworkScanner"/>.
    /// All tests use a pre-cancelled token combined with explicit (tiny) address ranges
    /// so no actual ICMP packets are sent.
    /// </summary>
    public class NetworkScannerTests
    {
        /// <summary>
        /// A single /32 address range containing 127.0.0.1.
        /// </summary>
        private static readonly AddressRange[] kLoopbackRange =
            [new AddressRange(0x7f000001u, 0x7f000001u)];

        [Fact]
        public void ScanCountStartsAtZero()
        {
            var logger = Mock.Of<ILogger<NetworkScanner>>();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            using var scanner = new NetworkScanner(logger,
                (s, r) => { }, false, kLoopbackRange, NetworkClass.Wired, 1,
                TimeSpan.FromMilliseconds(10), cts.Token);

            Assert.Equal(0, scanner.ScanCount);
        }

        [Fact]
        public async Task PreCancelledTokenWithExplicitAddressCompletes()
        {
            var logger = Mock.Of<ILogger<NetworkScanner>>();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            using var scanner = new NetworkScanner(logger,
                (s, r) => { }, false, kLoopbackRange, NetworkClass.Wired, 2,
                TimeSpan.FromMilliseconds(10), cts.Token);

            await scanner.WaitToCompleteAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task ActiveProbesZeroAfterPreCancelledCompletion()
        {
            var logger = Mock.Of<ILogger<NetworkScanner>>();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            using var scanner = new NetworkScanner(logger,
                (s, r) => { }, false, kLoopbackRange, NetworkClass.Wired, 2,
                TimeSpan.FromMilliseconds(10), cts.Token);

            await scanner.WaitToCompleteAsync().ConfigureAwait(false);

            Assert.Equal(0, scanner.ActiveProbes);
        }

        [Fact]
        public async Task WaitToCompleteAsyncReturnsSameTask()
        {
            var logger = Mock.Of<ILogger<NetworkScanner>>();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            using var scanner = new NetworkScanner(logger,
                (s, r) => { }, false, kLoopbackRange, NetworkClass.Wired, 1,
                TimeSpan.FromMilliseconds(10), cts.Token);

            var t1 = scanner.WaitToCompleteAsync();
            var t2 = scanner.WaitToCompleteAsync();
            Assert.Same(t1, t2);

            await t1.ConfigureAwait(false);
        }

        [Fact]
        public async Task DisposeAfterCompletionDoesNotThrow()
        {
            var logger = Mock.Of<ILogger<NetworkScanner>>();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var scanner = new NetworkScanner(logger,
                (s, r) => { }, false, kLoopbackRange, NetworkClass.Wired, 2,
                TimeSpan.FromMilliseconds(10), cts.Token);

            await scanner.WaitToCompleteAsync().ConfigureAwait(false);

            var ex = Record.Exception(() => scanner.Dispose());
            Assert.Null(ex);
        }

        [Fact]
        public async Task MultipleProbeCountPreCancelledCompletes()
        {
            var logger = Mock.Of<ILogger<NetworkScanner>>();
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            // 5 probes, pre-cancelled → all exit immediately
            var addresses = new[]
            {
                new AddressRange(0x7f000001u, 0x7f000005u) // 127.0.0.1 – 127.0.0.5
            };

            using var scanner = new NetworkScanner(logger,
                (s, r) => { }, false, addresses, NetworkClass.Wired, 5,
                TimeSpan.FromMilliseconds(10), cts.Token);

            await scanner.WaitToCompleteAsync().ConfigureAwait(false);
        }
    }
}
