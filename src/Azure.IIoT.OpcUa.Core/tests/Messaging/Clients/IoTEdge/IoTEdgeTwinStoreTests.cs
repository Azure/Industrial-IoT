// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.IoTEdge
{
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using System;
    using System.Text.Json.Nodes;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class IoTEdgeTwinStoreTests
    {
        [Fact]
        public void ConstructorRejectsNullClientBeforeStartingSynchronization()
        {
            Assert.Throws<ArgumentNullException>(() => new IoTEdgeTwinStore(
                null!, NullLogger<IoTEdgeTwinStore>.Instance));
        }

        [Fact]
        public async Task AwaiterLoadsReportedAndDesiredTwinPropertiesAsync()
        {
            var sdk = new IoTEdgeTestModuleClient
            {
                Twin = IoTEdgeTestModuleClient.CreateTwin(
                    """{"desired":1,"$version":2}""",
                    """{"reported":2,"desired":3,"$metadata":{}}""")
            };
            await using var store = CreateStore(sdk);

            await store;

            Assert.Equal("1", store["desired"]!.ToJsonString());
            Assert.Equal("2", store["reported"]!.ToJsonString());
            Assert.False(store.ContainsKey("$version"));
            Assert.False(store.ContainsKey("$metadata"));
            Assert.Equal(1, sdk.ConnectCount);
        }

        [Fact]
        public async Task TryPageInPrefersReportedOverDesiredAndUpdatesStateAsync()
        {
            var sdk = new IoTEdgeTestModuleClient
            {
                Twin = IoTEdgeTestModuleClient.CreateTwin(
                    """{"setting":"desired"}""",
                    """{"setting":"reported"}""")
            };
            await using var store = CreateStore(sdk);

            var value = await store.TryPageInAsync("setting");

            Assert.Equal("\"reported\"", value!.ToJsonString());
            Assert.Equal("\"reported\"", store["setting"]!.ToJsonString());
        }

        [Fact]
        public async Task TryPageInFallsBackToDesiredAndCachesNullForMissingAsync()
        {
            var sdk = new IoTEdgeTestModuleClient
            {
                Twin = IoTEdgeTestModuleClient.CreateTwin(
                    """{"setting":"desired"}""",
                    "{}")
            };
            await using var store = CreateStore(sdk);

            var value = await store.TryPageInAsync("setting");
            var missing = await store.TryPageInAsync("missing");

            Assert.Equal("\"desired\"", value!.ToJsonString());
            Assert.Null(missing);
            Assert.True(store.ContainsKey("missing"));
            Assert.Null(store["missing"]);
        }

        [Fact]
        public async Task WritesSynchronizeReportedPropertyPatchAsync()
        {
            var sdk = new IoTEdgeTestModuleClient();
            await using var store = CreateStore(sdk);
            await store;

            store["setting"] = JsonValue.Create("value");

            var patch = await sdk.ReportedPropertyPatches.Reader
                .ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal("""{"setting":"value"}""", patch);
        }

        [Fact]
        public async Task RemovedValuesSynchronizeReportedPropertyNullAsync()
        {
            var sdk = new IoTEdgeTestModuleClient
            {
                Twin = IoTEdgeTestModuleClient.CreateTwin("{}",
                    """{"setting":"value"}""")
            };
            await using var store = CreateStore(sdk);
            await store;

            var removed = store.Remove("setting");

            var patch = await sdk.ReportedPropertyPatches.Reader
                .ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));
            Assert.True(removed);
            Assert.Equal("""{"setting":null}""", patch);
        }

        private static IoTEdgeTwinStore CreateStore(IoTEdgeTestModuleClient sdk)
        {
            var client = new IoTEdgeModuleClient(
                Options.Create(new IoTEdgeClientOptions()),
                new IoTEdgeTestIdentity(),
                [],
                clientFactory: new IoTEdgeTestModuleClientFactory(sdk));
            return new IoTEdgeTwinStore(client,
                NullLogger<IoTEdgeTwinStore>.Instance);
        }
    }
}
