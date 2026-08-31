// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack.Models
{
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Moq;
    using System;
    using System.Threading;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="ServiceCallContext"/>.
    /// All tests are pure unit tests — no OPC UA server required.
    /// </summary>
    public sealed class ServiceCallContextTests
    {
        // ── Simple constructor ─────────────────────────────────────────────────

        [Fact]
        public void SimpleConstructor_SetsSessionProperty()
        {
            var session = Mock.Of<IOpcUaSession>();
            var timeout = TimeSpan.FromSeconds(30);

            var ctx = new ServiceCallContext(session, timeout);

            Assert.Same(session, ctx.Session);
        }

        [Fact]
        public void SimpleConstructor_SetsServiceCallTimeout()
        {
            var session = Mock.Of<IOpcUaSession>();
            var timeout = TimeSpan.FromSeconds(42);

            var ctx = new ServiceCallContext(session, timeout);

            Assert.Equal(TimeSpan.FromSeconds(42), ctx.ServiceCallTimeout);
        }

        [Fact]
        public void SimpleConstructor_SetsDefaultCancellationToken()
        {
            var session = Mock.Of<IOpcUaSession>();

            var ctx = new ServiceCallContext(session, TimeSpan.FromSeconds(5));

            Assert.Equal(CancellationToken.None, ctx.Ct);
        }

        [Fact]
        public void SimpleConstructor_SetsCancellationToken()
        {
            var session = Mock.Of<IOpcUaSession>();
            using var cts = new CancellationTokenSource();

            var ctx = new ServiceCallContext(session, TimeSpan.FromSeconds(5), cts.Token);

            Assert.Equal(cts.Token, ctx.Ct);
        }

        [Fact]
        public void SimpleConstructor_TrackedTokenIsNullByDefault()
        {
            var session = Mock.Of<IOpcUaSession>();

            var ctx = new ServiceCallContext(session, TimeSpan.FromSeconds(5));

            Assert.Null(ctx.TrackedToken);
        }

        [Fact]
        public void SimpleConstructor_UntrackedTokenIsNullByDefault()
        {
            var session = Mock.Of<IOpcUaSession>();

            var ctx = new ServiceCallContext(session, TimeSpan.FromSeconds(5));

            Assert.Null(ctx.UntrackedToken);
        }

        [Fact]
        public void TrackedToken_CanBeSetAfterConstruction()
        {
            var session = Mock.Of<IOpcUaSession>();
            var ctx = new ServiceCallContext(session, TimeSpan.FromSeconds(5));

            ctx.TrackedToken = "my-token";

            Assert.Equal("my-token", ctx.TrackedToken);
        }

        [Fact]
        public void UntrackedToken_CanBeSetAfterConstruction()
        {
            var session = Mock.Of<IOpcUaSession>();
            var ctx = new ServiceCallContext(session, TimeSpan.FromSeconds(5));

            ctx.UntrackedToken = "release-token";

            Assert.Equal("release-token", ctx.UntrackedToken);
        }

        // ── Simple constructor Dispose ─────────────────────────────────────────

        [Fact]
        public void SimpleConstructor_Dispose_DoesNotThrow()
        {
            var session = Mock.Of<IOpcUaSession>();
            var ctx = new ServiceCallContext(session, TimeSpan.FromSeconds(5));

            var ex = Record.Exception(() => ctx.Dispose());

            Assert.Null(ex);
        }

        [Fact]
        public void SimpleConstructor_DoubleDispose_DoesNotThrow()
        {
            var session = Mock.Of<IOpcUaSession>();
            var ctx = new ServiceCallContext(session, TimeSpan.FromSeconds(5));

            ctx.Dispose();
            var ex = Record.Exception(() => ctx.Dispose());

            Assert.Null(ex);
        }

        // ── Complex constructor with addRef / release ──────────────────────────

        [Fact]
        public void ComplexConstructor_CallsAddRefImmediately()
        {
            var session = Mock.Of<IOpcUaSession>();
            var sessionLock = new Mock<IDisposable>();
            var addRefCallCount = 0;

            _ = new ServiceCallContext(session, TimeSpan.FromSeconds(5),
                () => addRefCallCount++,
                () => { },
                sessionLock.Object);

            Assert.Equal(1, addRefCallCount);
        }

        [Fact]
        public void ComplexConstructor_DoesNotCallReleaseBeforeDispose()
        {
            var session = Mock.Of<IOpcUaSession>();
            var sessionLock = new Mock<IDisposable>();
            var releaseCallCount = 0;

            _ = new ServiceCallContext(session, TimeSpan.FromSeconds(5),
                () => { },
                () => releaseCallCount++,
                sessionLock.Object);

            Assert.Equal(0, releaseCallCount);
        }

        [Fact]
        public void ComplexConstructor_Dispose_CallsRelease()
        {
            var session = Mock.Of<IOpcUaSession>();
            var sessionLock = new Mock<IDisposable>();
            var releaseCallCount = 0;

            var ctx = new ServiceCallContext(session, TimeSpan.FromSeconds(5),
                () => { },
                () => releaseCallCount++,
                sessionLock.Object);

            ctx.Dispose();

            Assert.Equal(1, releaseCallCount);
        }

        [Fact]
        public void ComplexConstructor_Dispose_DisposesSessionLock()
        {
            var session = Mock.Of<IOpcUaSession>();
            var sessionLock = new Mock<IDisposable>();

            var ctx = new ServiceCallContext(session, TimeSpan.FromSeconds(5),
                () => { },
                () => { },
                sessionLock.Object);

            ctx.Dispose();

            sessionLock.Verify(l => l.Dispose(), Times.Once);
        }

        [Fact]
        public void ComplexConstructor_DoubleDispose_CallsReleaseOnlyOnce()
        {
            var session = Mock.Of<IOpcUaSession>();
            var sessionLock = new Mock<IDisposable>();
            var releaseCallCount = 0;

            var ctx = new ServiceCallContext(session, TimeSpan.FromSeconds(5),
                () => { },
                () => releaseCallCount++,
                sessionLock.Object);

            ctx.Dispose();
            ctx.Dispose();

            Assert.Equal(1, releaseCallCount);
        }

        [Fact]
        public void ComplexConstructor_DoubleDispose_DisposesSessionLockOnlyOnce()
        {
            var session = Mock.Of<IOpcUaSession>();
            var sessionLock = new Mock<IDisposable>();

            var ctx = new ServiceCallContext(session, TimeSpan.FromSeconds(5),
                () => { },
                () => { },
                sessionLock.Object);

            ctx.Dispose();
            ctx.Dispose();

            sessionLock.Verify(l => l.Dispose(), Times.Once);
        }

        [Fact]
        public void ComplexConstructor_SetsSessionProperty()
        {
            var session = Mock.Of<IOpcUaSession>();
            var sessionLock = Mock.Of<IDisposable>();

            using var ctx = new ServiceCallContext(session, TimeSpan.FromSeconds(5),
                () => { },
                () => { },
                sessionLock);

            Assert.Same(session, ctx.Session);
        }

        [Fact]
        public void ComplexConstructor_SetsServiceCallTimeout()
        {
            var session = Mock.Of<IOpcUaSession>();
            var sessionLock = Mock.Of<IDisposable>();
            var timeout = TimeSpan.FromMinutes(2);

            using var ctx = new ServiceCallContext(session, timeout,
                () => { },
                () => { },
                sessionLock);

            Assert.Equal(timeout, ctx.ServiceCallTimeout);
        }

        [Fact]
        public void ComplexConstructor_WithCancellationToken_SetsCt()
        {
            var session = Mock.Of<IOpcUaSession>();
            var sessionLock = Mock.Of<IDisposable>();
            using var cts = new CancellationTokenSource();

            using var ctx = new ServiceCallContext(session, TimeSpan.FromSeconds(5),
                () => { },
                () => { },
                sessionLock,
                cts.Token);

            Assert.Equal(cts.Token, ctx.Ct);
        }
    }
}
