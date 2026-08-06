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

        // ── GetNext exception paths ────────────────────────────────────────────

        [Fact]
        public void GetNext_ThrowsOperationCanceledException_CallsOnExit()
        {
            var logger = Mock.Of<ILogger>();
            var probe = new ThrowingConnectProbe(0, new NullAsyncProbe(), logger,
                new OperationCanceledException("cancelled"));

            probe.Start();

            Assert.Equal(1, probe.ExitCount);
            Assert.Equal(0, probe.FailCount);
        }

        [Fact]
        public void GetNext_ThrowsInvalidOperationException_ThenReturnsFalse_CallsOnExit()
        {
            // InvalidOperationException → continue (retry same GetNext loop),
            // second call returns false → exit = true → OnExit()
            var logger = Mock.Of<ILogger>();
            var probe = new InvalidOpThenFalseConnectProbe(0, new NullAsyncProbe(), logger);

            probe.Start();

            Assert.Equal(1, probe.ExitCount);
            Assert.Equal(0, probe.FailCount);
        }

        [Fact]
        public void GetNext_ThrowsUnexpectedException_CallsOnExit()
        {
            var logger = Mock.Of<ILogger>();
            var probe = new ThrowingConnectProbe(0, new NullAsyncProbe(), logger,
                new ArgumentException("unexpected"));

            probe.Start();

            Assert.Equal(1, probe.ExitCount);
            Assert.Equal(0, probe.FailCount);
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

        /// <summary>Throws a configurable exception from GetNext.</summary>
        private sealed class ThrowingConnectProbe : BaseConnectProbe
        {
            private readonly Exception _ex;

            public int ExitCount { get; private set; }
            public int FailCount { get; private set; }

            public ThrowingConnectProbe(int index, IAsyncProbe probe, ILogger logger,
                Exception ex) : base(index, probe, logger)
            {
                _ex = ex;
            }

            protected override bool GetNext(
                [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out IPEndPoint? ep,
                out int timeout)
            {
                ep = null;
                timeout = 0;
                throw _ex;
            }

            protected override void OnFail(IPEndPoint ep) => FailCount++;
            protected override void OnSuccess(IPEndPoint ep) { }
            protected override void OnExit() => ExitCount++;
        }

        /// <summary>
        /// Throws <see cref="InvalidOperationException"/> on the first call,
        /// then returns false on the second call.
        /// </summary>
        private sealed class InvalidOpThenFalseConnectProbe : BaseConnectProbe
        {
            private int _callCount;

            public int ExitCount { get; private set; }
            public int FailCount { get; private set; }

            public InvalidOpThenFalseConnectProbe(int index, IAsyncProbe probe, ILogger logger)
                : base(index, probe, logger)
            {
            }

            protected override bool GetNext(
                [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out IPEndPoint? ep,
                out int timeout)
            {
                ep = null;
                timeout = 0;
                if (_callCount++ == 0)
                {
                    throw new InvalidOperationException("retry");
                }
                return false;
            }

            protected override void OnFail(IPEndPoint ep) => FailCount++;
            protected override void OnSuccess(IPEndPoint ep) { }
            protected override void OnExit() => ExitCount++;
        }
    }
}
