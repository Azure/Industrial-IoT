// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Storage.Services
{
    using System.Text.Json.Nodes;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class MemoryKVStoreTests
    {
        [Fact]
        public void NameIdentifiesMemoryStore()
        {
            var store = new MemoryKVStore();

            Assert.Equal("Memory", store.Name);
        }

        [Fact]
        public async Task TryPageInAsyncReturnsNullForMissingKeyAsync()
        {
            var store = new MemoryKVStore();

            var value = await store.TryPageInAsync("missing");

            Assert.Null(value);
        }

        [Fact]
        public async Task TryPageInAsyncReturnsStoredJsonNodeAsync()
        {
            var store = new MemoryKVStore();
            var expected = JsonNode.Parse("""{"value":42}""");
            store.State["key"] = expected;

            var value = await store.TryPageInAsync("key");

            Assert.Equal(expected!.ToJsonString(), value!.ToJsonString());
        }

        [Fact]
        public async Task StateAllowsNullValuesAsync()
        {
            var store = new MemoryKVStore();
            store.State["key"] = null;

            var value = await store.TryPageInAsync("key");

            Assert.Null(value);
            Assert.Contains("key", store.State.Keys);
        }
    }
}
