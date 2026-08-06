// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack.Transport.Scanner
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Transport;
    using Azure.IIoT.OpcUa.Publisher.Stack.Transport.Scanner;
    using Microsoft.Extensions.Logging;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class PortScannerTests
    {
        [Fact]
        public void NullLoggerThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new PortScanner(null!, [], (s, ep) => { }, CancellationToken.None));
        }

        [Fact]
        public void NullSourceThrowsArgumentNullException()
        {
            var logger = Mock.Of<ILogger<PortScanner>>();
            Assert.Throws<ArgumentNullException>(() =>
                new PortScanner(logger, null!, (s, ep) => { }, CancellationToken.None));
        }

        [Fact]
        public void NullTargetThrowsArgumentNullException()
        {
            var logger = Mock.Of<ILogger<PortScanner>>();
            Assert.Throws<ArgumentNullException>(() =>
                new PortScanner(logger, [], null!, CancellationToken.None));
        }

        [Fact]
        public async Task EmptySourceCompletesWithoutCallingTarget()
        {
            var logger = Mock.Of<ILogger<PortScanner>>();
            var found = new List<IPEndPoint>();
            using var scanner = new PortScanner(logger, [],
                (s, ep) => found.Add(ep),
                null, 2, 50, TimeSpan.FromMilliseconds(100), CancellationToken.None);

            await scanner.WaitToCompleteAsync().ConfigureAwait(false);

            Assert.Empty(found);
            Assert.Equal(0, scanner.ScanCount);
        }

        [Fact]
        public async Task ScanCountIsZeroWithEmptySource()
        {
            var logger = Mock.Of<ILogger<PortScanner>>();
            using var scanner = new PortScanner(logger, [],
                (s, ep) => { },
                null, 2, 50, TimeSpan.FromMilliseconds(100), CancellationToken.None);

            await scanner.WaitToCompleteAsync().ConfigureAwait(false);

            Assert.Equal(0, scanner.ScanCount);
        }

        [Fact]
        public async Task ActiveProbesIsZeroAfterEmptySourceCompletes()
        {
            var logger = Mock.Of<ILogger<PortScanner>>();
            using var scanner = new PortScanner(logger, [],
                (s, ep) => { },
                null, 5, 50, TimeSpan.FromMilliseconds(100), CancellationToken.None);

            await scanner.WaitToCompleteAsync().ConfigureAwait(false);

            Assert.Equal(0, scanner.ActiveProbes);
        }

        [Fact]
        public async Task WaitToCompleteAsyncReturnsSameTaskEachTime()
        {
            var logger = Mock.Of<ILogger<PortScanner>>();
            using var scanner = new PortScanner(logger, [],
                (s, ep) => { },
                null, 2, 50, TimeSpan.FromMilliseconds(100), CancellationToken.None);

            var t1 = scanner.WaitToCompleteAsync();
            var t2 = scanner.WaitToCompleteAsync();
            Assert.Same(t1, t2);

            await t1.ConfigureAwait(false);
        }

        [Fact]
        public void DisposeCanBeCalledBeforeWaitToComplete()
        {
            var logger = Mock.Of<ILogger<PortScanner>>();
            var scanner = new PortScanner(logger, [],
                (s, ep) => { },
                null, 2, 50, TimeSpan.FromMilliseconds(100), CancellationToken.None);

            var ex = Record.Exception(() => scanner.Dispose());
            Assert.Null(ex);
        }

        [Fact]
        public async Task FourArgConstructorDelegatesToFullConstructorAsync()
        {
            // Exercises the 4-arg overload: PortScanner(logger, source, target, ct)
            var logger = Mock.Of<ILogger<PortScanner>>();
            using var scanner = new PortScanner(logger, [],
                (s, ep) => { }, CancellationToken.None);

            await scanner.WaitToCompleteAsync().ConfigureAwait(false);

            Assert.Equal(0, scanner.ScanCount);
        }

        [Fact]
        public async Task FiveArgConstructorWithPortProbeDelegatesToFullConstructorAsync()
        {
            // Exercises the 5-arg overload: PortScanner(logger, source, target, portProbe, ct)
            var logger = Mock.Of<ILogger<PortScanner>>();
            using var scanner = new PortScanner(logger, [],
                (s, ep) => { }, null, CancellationToken.None);

            await scanner.WaitToCompleteAsync().ConfigureAwait(false);

            Assert.Equal(0, scanner.ScanCount);
        }
    }
}
