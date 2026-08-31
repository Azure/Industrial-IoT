// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.EventHubs
{
    using Azure.IIoT.OpcUa.Core.Configuration;
    using Microsoft.Extensions.Configuration;
    using System.Collections.Generic;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="EventHubsClientConfig"/>.
    /// </summary>
    public sealed class EventHubsClientConfigTests
    {
        private static EventHubsClientOptions Configure(params KeyValuePair<string, string?>[] pairs)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(pairs)
                .Build();
            return new EventHubsClientConfig(configuration).ToOptions().Value;
        }

        private static KeyValuePair<string, string?> P(string k, string? v) => new(k, v);

        // ── Bind / ConnectionString ───────────────────────────────────────────

        [Fact]
        public void Defaults_ConnectionString_IsEmpty()
        {
            var opts = Configure();
            // PostConfigure sets empty string when nothing is configured
            Assert.True(string.IsNullOrEmpty(opts.ConnectionString));
        }

        [Fact]
        public void Config_ConnectionString_SetsValue()
        {
            var opts = Configure(P(nameof(EventHubsClientOptions.ConnectionString),
                "Endpoint=sb://ns.servicebus.windows.net/;SharedAccessKeyName=k;SharedAccessKey=v"));
            Assert.Equal(
                "Endpoint=sb://ns.servicebus.windows.net/;SharedAccessKeyName=k;SharedAccessKey=v",
                opts.ConnectionString);
        }

        // ── Bind / MaxEventPayloadSizeInBytes ─────────────────────────────────

        [Fact]
        public void Defaults_MaxEventPayloadSizeInBytes_IsNull()
        {
            var opts = Configure();
            Assert.Null(opts.MaxEventPayloadSizeInBytes);
        }

        [Fact]
        public void Config_MaxEventPayloadSizeInBytes_SetsValue()
        {
            var opts = Configure(P(nameof(EventHubsClientOptions.MaxEventPayloadSizeInBytes), "131072"));
            Assert.Equal(131072, opts.MaxEventPayloadSizeInBytes);
        }

        // ── Bind / SchemaRegistry ─────────────────────────────────────────────

        [Fact]
        public void Defaults_SchemaRegistry_IsNull()
        {
            var opts = Configure();
            Assert.Null(opts.SchemaRegistry);
        }

        [Fact]
        public void Config_SchemaRegistry_SetsWhenBothKeysPresent()
        {
            var opts = Configure(
                P($"{nameof(EventHubsClientOptions.SchemaRegistry)}:{nameof(SchemaRegistryOptions.FullyQualifiedNamespace)}",
                    "my-ns.servicebus.windows.net"),
                P($"{nameof(EventHubsClientOptions.SchemaRegistry)}:{nameof(SchemaRegistryOptions.SchemaGroupName)}",
                    "my-group"));
            Assert.NotNull(opts.SchemaRegistry);
            Assert.Equal("my-ns.servicebus.windows.net", opts.SchemaRegistry.FullyQualifiedNamespace);
            Assert.Equal("my-group", opts.SchemaRegistry.SchemaGroupName);
        }

        [Fact]
        public void Config_SchemaRegistry_PopulatedWhenOnlyNamespacePresent()
        {
            // If either key is present the object is created (both-null → null)
            var opts = Configure(
                P($"{nameof(EventHubsClientOptions.SchemaRegistry)}:{nameof(SchemaRegistryOptions.FullyQualifiedNamespace)}",
                    "my-ns.servicebus.windows.net"));
            Assert.NotNull(opts.SchemaRegistry);
            Assert.Equal("my-ns.servicebus.windows.net", opts.SchemaRegistry!.FullyQualifiedNamespace);
        }

        // ── PostConfigure / ConnectionString from env var ─────────────────────

        [Fact]
        public void PostConfigure_ConnectionString_SetFromPcsEnvVar()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection([P("PCS_EVENTHUB_CONNECTIONSTRING",
                    "Endpoint=sb://pcs.servicebus.windows.net/;SharedAccessKeyName=k;SharedAccessKey=v")])
                .Build();
            var opts = new EventHubsClientConfig(configuration).ToOptions().Value;
            Assert.Equal(
                "Endpoint=sb://pcs.servicebus.windows.net/;SharedAccessKeyName=k;SharedAccessKey=v",
                opts.ConnectionString);
        }

        [Fact]
        public void PostConfigure_ConnectionString_SetFromShortEnvVar()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection([P("_EH_CS",
                    "Endpoint=sb://short.servicebus.windows.net/;SharedAccessKeyName=k;SharedAccessKey=v")])
                .Build();
            var opts = new EventHubsClientConfig(configuration).ToOptions().Value;
            Assert.Equal(
                "Endpoint=sb://short.servicebus.windows.net/;SharedAccessKeyName=k;SharedAccessKey=v",
                opts.ConnectionString);
        }

        [Fact]
        public void PostConfigure_ConnectionString_NotOverriddenWhenAlreadyBound()
        {
            var cs = "Endpoint=sb://preset.servicebus.windows.net/;SharedAccessKeyName=k;SharedAccessKey=v";
            var opts = Configure(
                P(nameof(EventHubsClientOptions.ConnectionString), cs),
                P("PCS_EVENTHUB_CONNECTIONSTRING", "Endpoint=sb://env.servicebus.windows.net/;SharedAccessKeyName=k;SharedAccessKey=v"));
            Assert.Equal(cs, opts.ConnectionString);
        }
    }
}
