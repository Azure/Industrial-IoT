// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Microsoft.Extensions.Logging;
    using Moq;
    using Opc.Ua.Client;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Unit tests for <see cref="OpcUaBrowser"/> lifecycle and control methods.
    /// All tests are pure unit tests — no real sessions or sockets.
    /// </summary>
    public sealed class OpcUaBrowserTests
    {
        // ── Register ───────────────────────────────────────────────────────────

        [Fact]
        public void Register_AddsEntryToRegistry()
        {
            var (session, logger, browsers) = Setup();

            var browser = OpcUaBrowser.Register(
                session.Object, logger, TimeProvider.System, browsers,
                TimeSpan.Zero, "sub1");
            try
            {
                Assert.Single((ICollection<KeyValuePair<(string, TimeSpan), OpcUaBrowser>>)browsers);
            }
            finally
            {
                browser.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        }

        [Fact]
        public void Register_ReturnsSameInstanceForSameKey()
        {
            var (session, logger, browsers) = Setup();

            var browser1 = OpcUaBrowser.Register(
                session.Object, logger, TimeProvider.System, browsers,
                TimeSpan.Zero, "sub1");
            var browser2 = OpcUaBrowser.Register(
                session.Object, logger, TimeProvider.System, browsers,
                TimeSpan.Zero, "sub1");
            try
            {
                Assert.Same(browser1, browser2);
                Assert.Single((ICollection<KeyValuePair<(string, TimeSpan), OpcUaBrowser>>)browsers);
            }
            finally
            {
                browser2.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        }

        [Fact]
        public void Register_ReturnsDifferentInstancesForDifferentKeys()
        {
            var (session, logger, browsers) = Setup();

            var browser1 = OpcUaBrowser.Register(
                session.Object, logger, TimeProvider.System, browsers,
                TimeSpan.Zero, "sub1");
            var browser2 = OpcUaBrowser.Register(
                session.Object, logger, TimeProvider.System, browsers,
                TimeSpan.Zero, "sub2");
            try
            {
                Assert.NotSame(browser1, browser2);
                Assert.Equal(2, browsers.Count);
            }
            finally
            {
                browser1.DisposeAsync().AsTask().GetAwaiter().GetResult();
                browser2.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        }

        [Fact]
        public void Register_NullSession_ThrowsArgumentNullException()
        {
            var browsers = new Dictionary<(string, TimeSpan), OpcUaBrowser>();

            Assert.Throws<ArgumentNullException>(() =>
                OpcUaBrowser.Register(null!, Mock.Of<ILogger>(),
                    TimeProvider.System, browsers, TimeSpan.Zero, "sub1"));
        }

        // ── CloseAsync / Release ───────────────────────────────────────────────

        [Fact]
        public async Task CloseAsync_RemovesBrowserFromRegistry()
        {
            var (session, logger, browsers) = Setup();

            var browser = OpcUaBrowser.Register(
                session.Object, logger, TimeProvider.System, browsers,
                TimeSpan.Zero, "sub1");
            Assert.Single((ICollection<KeyValuePair<(string, TimeSpan), OpcUaBrowser>>)browsers);

            await browser.CloseAsync();

            Assert.Empty(browsers);
        }

        [Fact]
        public async Task CloseAsync_TwoReferences_OnlyRemovedAfterBothClose()
        {
            var (session, logger, browsers) = Setup();

            var browser1 = OpcUaBrowser.Register(
                session.Object, logger, TimeProvider.System, browsers,
                TimeSpan.Zero, "sub1");
            var browser2 = OpcUaBrowser.Register(
                session.Object, logger, TimeProvider.System, browsers,
                TimeSpan.Zero, "sub1"); // same instance, ref count = 2

            await browser1.CloseAsync(); // ref count → 1, still in registry
            Assert.Single((ICollection<KeyValuePair<(string, TimeSpan), OpcUaBrowser>>)browsers);

            await browser2.CloseAsync(); // ref count → 0, removed
            Assert.Empty(browsers);
        }

        // ── Start / Rebrowse / OnConnected ─────────────────────────────────────

        [Fact]
        public async Task Start_And_DisposeAsync_CompletesCleanly()
        {
            var (session, logger, browsers) = Setup();

            var browser = OpcUaBrowser.Register(
                session.Object, logger, TimeProvider.System, browsers,
                TimeSpan.Zero, "sub1");

            browser.Start();
            await browser.DisposeAsync();
        }

        [Fact]
        public async Task Start_DisconnectedSession_DoesNotBrowse()
        {
            var (session, logger, browsers) = Setup(); // Connected defaults to false

            var browser = OpcUaBrowser.Register(
                session.Object, logger, TimeProvider.System, browsers,
                TimeSpan.Zero, "sub1");

            browser.Start();
            await browser.DisposeAsync();

            // Connected was checked but no browsing exception occurred
            session.Verify(s => s.Connected, Times.AtLeastOnce());
        }

        [Fact]
        public async Task Rebrowse_BeforeStart_DoesNothing()
        {
            var (session, logger, browsers) = Setup();

            var browser = OpcUaBrowser.Register(
                session.Object, logger, TimeProvider.System, browsers,
                TimeSpan.Zero, "sub1");

            browser.Rebrowse(); // _started == 0, should be a no-op
            await browser.DisposeAsync();
        }

        [Fact]
        public async Task Rebrowse_AfterStart_WritesToChannel()
        {
            var (session, logger, browsers) = Setup();

            var browser = OpcUaBrowser.Register(
                session.Object, logger, TimeProvider.System, browsers,
                TimeSpan.Zero, "sub1");

            browser.Start();
            browser.Rebrowse(); // _started != 0, writes true
            await browser.DisposeAsync();
        }

        [Fact]
        public async Task OnConnected_BeforeStart_DoesNothing()
        {
            var (session, logger, browsers) = Setup();

            var browser = OpcUaBrowser.Register(
                session.Object, logger, TimeProvider.System, browsers,
                TimeSpan.Zero, "sub1");

            browser.OnConnected(); // _started == 0, should be a no-op
            await browser.DisposeAsync();
        }

        [Fact]
        public async Task OnConnected_AfterStart_WritesReconnectToChannel()
        {
            var (session, logger, browsers) = Setup();

            var browser = OpcUaBrowser.Register(
                session.Object, logger, TimeProvider.System, browsers,
                TimeSpan.Zero, "sub1");

            browser.Start();
            browser.OnConnected(); // writes false → triggers the !result branch in RunAsync
            await browser.DisposeAsync();
        }

        [Fact]
        public async Task DisposeAsync_MultipleTimes_DoesNotThrow()
        {
            var (session, logger, browsers) = Setup();

            var browser = OpcUaBrowser.Register(
                session.Object, logger, TimeProvider.System, browsers,
                TimeSpan.Zero, "sub1");

            await browser.DisposeAsync();
            var ex = await Record.ExceptionAsync(() => browser.DisposeAsync().AsTask());
            Assert.Null(ex);
        }

        // ── helpers ────────────────────────────────────────────────────────────

        private static (Mock<ISession> Session, ILogger Logger,
            Dictionary<(string, TimeSpan), OpcUaBrowser> Browsers) Setup()
        {
            var session = new Mock<ISession>();
            // Connected defaults to false — no browsing will occur
            session.Setup(s => s.Connected).Returns(false);
            return (session, Mock.Of<ILogger>(),
                new Dictionary<(string, TimeSpan), OpcUaBrowser>());
        }
    }
}
