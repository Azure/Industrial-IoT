// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.Dapr
{
    using Azure.IIoT.OpcUa.Core.Configuration;
    using Microsoft.Extensions.Configuration;
    using System.Collections.Generic;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="DaprConfig"/>.
    /// </summary>
    public sealed class DaprConfigTests
    {
        private static DaprOptions Configure(params KeyValuePair<string, string?>[] pairs)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(pairs)
                .Build();
            return new DaprConfig(configuration).ToOptions().Value;
        }

        private static KeyValuePair<string, string?> P(string k, string? v) => new(k, v);

        // ── Bind / PubSubComponent ────────────────────────────────────────────

        [Fact]
        public void Defaults_PubSubComponent_IsNull()
        {
            var opts = Configure();
            Assert.Null(opts.PubSubComponent);
        }

        [Fact]
        public void Config_PubSubComponent_SetsValue()
        {
            var opts = Configure(P(nameof(DaprOptions.PubSubComponent), "my-pubsub"));
            Assert.Equal("my-pubsub", opts.PubSubComponent);
        }

        // ── Bind / StateStoreName ─────────────────────────────────────────────

        [Fact]
        public void Defaults_StateStoreName_IsNull()
        {
            var opts = Configure();
            Assert.Null(opts.StateStoreName);
        }

        [Fact]
        public void Config_StateStoreName_SetsValue()
        {
            var opts = Configure(P(nameof(DaprOptions.StateStoreName), "my-store"));
            Assert.Equal("my-store", opts.StateStoreName);
        }

        // ── Bind / MessageMaxBytes ────────────────────────────────────────────

        [Fact]
        public void Defaults_MessageMaxBytes_IsNull()
        {
            var opts = Configure();
            Assert.Null(opts.MessageMaxBytes);
        }

        [Fact]
        public void Config_MessageMaxBytes_SetsValue()
        {
            var opts = Configure(P(nameof(DaprOptions.MessageMaxBytes), "65536"));
            Assert.Equal(65536, opts.MessageMaxBytes);
        }

        // ── Bind / CheckSideCarHealthBeforeAccess ─────────────────────────────

        [Fact]
        public void Defaults_CheckSideCarHealth_IsFalse()
        {
            var opts = Configure();
            Assert.False(opts.CheckSideCarHealthBeforeAccess);
        }

        [Fact]
        public void Config_CheckSideCarHealth_SetsTrue()
        {
            var opts = Configure(P(nameof(DaprOptions.CheckSideCarHealthBeforeAccess), "true"));
            Assert.True(opts.CheckSideCarHealthBeforeAccess);
        }

        // ── PostConfigure / ApiToken ──────────────────────────────────────────

        [Fact]
        public void PostConfigure_ApiToken_SetFromConfig()
        {
            var opts = Configure(P(nameof(DaprOptions.ApiToken), "tok"));
            Assert.Equal("tok", opts.ApiToken);
        }

        [Fact]
        public void PostConfigure_ApiToken_NotOverriddenWhenAlreadyBound()
        {
            // When bound via config key ApiToken is pre-populated; PostConfigure skips it.
            var opts = Configure(
                P(nameof(DaprOptions.ApiToken), "from-bind"),
                P(EnvironmentVariable.DAPRAPITOKEN, "from-env"));
            Assert.Equal("from-bind", opts.ApiToken);
        }

        // ── PostConfigure / GrpcChannelOptions ───────────────────────────────

        [Fact]
        public void PostConfigure_GrpcChannelOptions_ThrowOnCancellationIsTrue()
        {
            var opts = Configure();
            Assert.True(opts.GrpcChannelOptions.ThrowOperationCanceledOnCancellation);
        }

        // ── PostConfigure / GrpcEndpoint from env var ─────────────────────────

        [Fact]
        public void PostConfigure_GrpcEndpoint_SetFromEnvVar()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection([P(EnvironmentVariable.DAPRGRPCENDPOINT, "http://localhost:50001")])
                .Build();
            var opts = new DaprConfig(configuration).ToOptions().Value;
            Assert.Equal("http://localhost:50001", opts.GrpcEndpoint);
        }

        [Fact]
        public void PostConfigure_GrpcEndpoint_NotOverriddenWhenAlreadySet()
        {
            var opts = Configure(
                P(nameof(DaprOptions.GrpcEndpoint), "http://preset:50001"),
                P(EnvironmentVariable.DAPRGRPCENDPOINT, "http://env:50001"));
            Assert.Equal("http://preset:50001", opts.GrpcEndpoint);
        }

        // ── PostConfigure / HttpEndpoint from env var ─────────────────────────

        [Fact]
        public void PostConfigure_HttpEndpoint_SetFromEnvVar()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection([P(EnvironmentVariable.DAPRHTTPENDPOINT, "http://localhost:3500")])
                .Build();
            var opts = new DaprConfig(configuration).ToOptions().Value;
            Assert.Equal("http://localhost:3500", opts.HttpEndpoint);
        }

        [Fact]
        public void PostConfigure_HttpEndpoint_NotOverriddenWhenAlreadySet()
        {
            var opts = Configure(
                P(nameof(DaprOptions.HttpEndpoint), "http://preset:3500"),
                P(EnvironmentVariable.DAPRHTTPENDPOINT, "http://env:3500"));
            Assert.Equal("http://preset:3500", opts.HttpEndpoint);
        }
    }
}
