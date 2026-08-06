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
    using System.Net.Sockets;
    using Xunit;

    public class BaseConnectProbeTests
    {
        [Fact]
        public void NullProbeThrowsArgumentNullException()
        {
            var logger = Mock.Of<ILogger>();
            Assert.Throws<ArgumentNullException>(() =>
                new TestConnectProbe(0, null!, logger));
        }

        [Fact]
        public void StartWithNoEndpointsCallsOnExitOnce()
        {
            var logger = Mock.Of<ILogger>();
            var probe = new TestConnectProbe(0, new NullAsyncProbe(), logger);

            probe.Start();

            Assert.Equal(1, probe.ExitCount);
            Assert.Equal(0, probe.SuccessCount);
            Assert.Equal(0, probe.FailCount);
        }

        [Fact]
        public void DisposeDoesNotThrow()
        {
            var logger = Mock.Of<ILogger>();
            var probe = new TestConnectProbe(0, new NullAsyncProbe(), logger);

            probe.Start();

            var ex = Record.Exception(() => probe.Dispose());
            Assert.Null(ex);
        }

        [Fact]
        public void DoubleDisposeDoesNotThrow()
        {
            var logger = Mock.Of<ILogger>();
            var probe = new TestConnectProbe(0, new NullAsyncProbe(), logger);

            probe.Dispose();
            var ex = Record.Exception(() => probe.Dispose());
            Assert.Null(ex);
        }

        [Fact]
        public void ShouldGiveUpDefaultIsFalse()
        {
            var logger = Mock.Of<ILogger>();
            using var probe = new TestConnectProbe(0, new NullAsyncProbe(), logger);

            // The default implementation returns false
            Assert.False(probe.PublicShouldGiveUp());
        }

        [Fact]
        public void ProbeIndexIsPreserved()
        {
            var logger = Mock.Of<ILogger>();
            using var probe0 = new TestConnectProbe(0, new NullAsyncProbe(), logger);
            using var probe1 = new TestConnectProbe(1, new NullAsyncProbe(), logger);
            using var probe7 = new TestConnectProbe(7, new NullAsyncProbe(), logger);

            Assert.Equal(0, probe0.Index);
            Assert.Equal(1, probe1.Index);
            Assert.Equal(7, probe7.Index);
        }

        [Fact]
        public void DisposeTwiceDoesNotThrow()
        {
            var logger = Mock.Of<ILogger>();
            var probe = new TestConnectProbe(0, new NullAsyncProbe(), logger);

            probe.Dispose();
            var ex = Record.Exception(() => probe.Dispose());
            Assert.Null(ex);
        }

        /// <summary>
        /// Concrete test subclass that has no endpoints and records callbacks.
        /// </summary>
        private sealed class TestConnectProbe : BaseConnectProbe
        {
            public int ExitCount { get; private set; }
            public int SuccessCount { get; private set; }
            public int FailCount { get; private set; }
            public int Index { get; }

            public TestConnectProbe(int index, IAsyncProbe probe, ILogger logger)
                : base(index, probe, logger)
            {
                Index = index;
            }

            public bool PublicShouldGiveUp() => ShouldGiveUp();

            protected override bool GetNext(
                [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out IPEndPoint? ep,
                out int timeout)
            {
                ep = null;
                timeout = 0;
                return false; // No endpoints → triggers exit
            }

            protected override void OnFail(IPEndPoint ep) => FailCount++;

            protected override void OnSuccess(IPEndPoint ep) => SuccessCount++;

            protected override void OnExit() => ExitCount++;
        }

        private sealed class NullAsyncProbe : IAsyncProbe
        {
            public bool OnComplete(int index, SocketAsyncEventArgs arg,
                out bool ok, out int timeout)
            {
                ok = true;
                timeout = 0;
                return true;
            }

            public bool Reset() => false;
            public void Dispose() { }
        }
    }
}
