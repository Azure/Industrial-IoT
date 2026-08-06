// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#nullable enable

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.Dapr
{
    using Microsoft.Extensions.Logging.Abstractions;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class SyncingKeyValueStoreTests
    {
        [Fact]
        public async Task AwaiterCompletesAfterInitialStateIsLoadedAsync()
        {
            await using var store = new TestStore
            {
                InitialState =
                {
                    ["present"] = JsonValue.Create(42)
                }
            };

            store.Start();
            var loaded = await store;

            Assert.Same(store, loaded);
            Assert.True(store.TryGetValue("present", out var value));
            Assert.Equal("42", value!.ToJsonString());
            Assert.False(store.TryGetValue("missing", out _));
        }

        [Fact]
        public async Task TryPageInUpdatesLocalStateWhenBackingValueExistsAsync()
        {
            await using var store = new TestStore
            {
                InitialState =
                {
                    ["present"] = JsonValue.Create("paged")
                }
            };

            var value = await store.TryPageInAsync("present");

            Assert.Equal("\"paged\"", value!.ToJsonString());
            Assert.True(store.TryGetValue("present", out var cached));
            Assert.Same(value, cached);
        }

        [Fact]
        public async Task TryPageInMissingValueLeavesStateAbsentAsync()
        {
            await using var store = new TestStore();

            var value = await store.TryPageInAsync("missing");

            Assert.Null(value);
            Assert.False(store.ContainsKey("missing"));
        }

        [Fact]
        public async Task WritingValueUpdatesLocalStateAndSynchronizesAsync()
        {
            await using var store = new TestStore();
            store.Start();
            await store;

            store["key"] = JsonValue.Create("value");

            Assert.True(store.TryGetValue("key", out var value));
            Assert.Equal("\"value\"", value!.ToJsonString());
            var batch = await store.ReadBatchAsync();
            Assert.Equal("\"value\"", batch["key"]!.ToJsonString());
        }

        [Fact]
        public async Task RepeatedWritesBeforeProcessorRunsCoalesceByKeyAsync()
        {
            await using var store = new TestStore(manualLoad: true);
            store.Start();

            store["key"] = JsonValue.Create(1);
            store["key"] = JsonValue.Create(2);
            store["other"] = JsonValue.Create(3);
            store.CompleteLoad();

            var batch = await store.ReadBatchAsync();
            Assert.Equal(2, batch.Count);
            Assert.Equal("2", batch["key"]!.ToJsonString());
            Assert.Equal("3", batch["other"]!.ToJsonString());
        }

        [Fact]
        public async Task RemoveExistingKeyUpdatesLocalStateAndSynchronizesTombstoneAsync()
        {
            await using var store = new TestStore
            {
                InitialState =
                {
                    ["key"] = JsonValue.Create("value")
                }
            };
            store.Start();
            await store;

            var removed = store.Remove("key");

            Assert.True(removed);
            Assert.False(store.ContainsKey("key"));
            var batch = await store.ReadBatchAsync();
            Assert.True(batch.ContainsKey("key"));
            Assert.Null(batch["key"]);
        }

        [Fact]
        public async Task RemoveMissingKeyDoesNotSynchronizeAsync()
        {
            await using var store = new TestStore();
            store.Start();
            await store;

            var removed = store.Remove("missing");
            store["present"] = JsonValue.Create(true);

            Assert.False(removed);
            var batch = await store.ReadBatchAsync();
            Assert.Equal(["present"], batch.Keys.ToArray());
        }

        [Fact]
        public async Task ClearSynchronizesTombstonesForExistingKeysAsync()
        {
            await using var store = new TestStore
            {
                InitialState =
                {
                    ["one"] = JsonValue.Create(1),
                    ["two"] = JsonValue.Create(2)
                }
            };
            store.Start();
            await store;

            store.Clear();

            Assert.Empty(store);
            var batch = await store.ReadBatchAsync();
            Assert.Equal(["one", "two"], batch.Keys.Order(StringComparer.Ordinal).ToArray());
            Assert.Null(batch["one"]);
            Assert.Null(batch["two"]);
        }

        [Fact]
        public async Task RemoveKeyValuePairRequiresMatchingCurrentValueAsync()
        {
            var current = JsonValue.Create("current");
            await using var store = new TestStore
            {
                InitialState =
                {
                    ["key"] = current
                }
            };
            store.Start();
            await store;

            var removed = store.Remove(new KeyValuePair<string, JsonNode?>("key",
                JsonValue.Create("different")));

            Assert.False(removed);
            Assert.True(store.ContainsKey("key"));
            Assert.Same(current, store["key"]);
        }

        [Fact]
        public async Task OnChangesExceptionIsContainedAndLocalStateRemainsAsync()
        {
            await using var store = new TestStore
            {
                ThrowOnChanges = true
            };
            store.Start();
            await store;

            store["key"] = JsonValue.Create("value");
            var batch = await store.ReadBatchAsync();

            Assert.Equal("\"value\"", batch["key"]!.ToJsonString());
            Assert.True(store.ContainsKey("key"));
        }

        [Fact]
        public async Task DisposeAsyncCancelsRunningProcessorAsync()
        {
            var store = new TestStore
            {
                BlockChangesUntilCancelled = true
            };
            store.Start();
            await store;
            store["key"] = JsonValue.Create("value");
            await store.ChangesEntered.Task;

            await store.DisposeAsync();

            Assert.True(store.ObservedChangeCancellation);
        }

        private sealed class TestStore : SyncingKeyValueStore
        {
            public TestStore(bool manualLoad = false) : base(NullLogger.Instance)
            {
                if (manualLoad)
                {
                    _load = new TaskCompletionSource(
                        TaskCreationOptions.RunContinuationsAsynchronously);
                }
            }

            public override string Name => "Test";
            public Dictionary<string, JsonNode?> InitialState { get; } = [];
            public bool ThrowOnChanges { get; init; }
            public bool BlockChangesUntilCancelled { get; init; }
            public bool ObservedChangeCancellation { get; private set; }
            public TaskCompletionSource ChangesEntered { get; } = new(
                TaskCreationOptions.RunContinuationsAsynchronously);

            public void Start()
            {
                StartStateSynchronization();
            }

            public void CompleteLoad()
            {
                _load?.SetResult();
            }

            public async Task<IDictionary<string, JsonNode?>> ReadBatchAsync()
            {
                return await _batches.Reader.ReadAsync().ConfigureAwait(false);
            }

            public override ValueTask<JsonNode?> TryPageInAsync(string key,
                CancellationToken ct = default)
            {
                if (InitialState.TryGetValue(key, out var value))
                {
                    ModifyState(state => state[key] = value);
                    return ValueTask.FromResult(value);
                }
                return ValueTask.FromResult<JsonNode?>(null);
            }

            protected override async ValueTask OnChangesAsync(
                IDictionary<string, JsonNode?> batch, CancellationToken ct)
            {
                var copy = batch.ToDictionary(item => item.Key, item => item.Value,
                    StringComparer.Ordinal);
                await _batches.Writer.WriteAsync(copy, ct).ConfigureAwait(false);
                ChangesEntered.TrySetResult();

                if (BlockChangesUntilCancelled)
                {
                    var cancelled = new TaskCompletionSource(
                        TaskCreationOptions.RunContinuationsAsynchronously);
                    using var registration = ct.Register(() =>
                        cancelled.TrySetCanceled(ct));
                    try
                    {
                        await cancelled.Task.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        ObservedChangeCancellation = true;
                        throw;
                    }
                }

                if (ThrowOnChanges)
                {
                    throw new InvalidOperationException("Backing store failed.");
                }
            }

            protected override async Task OnLoadState(CancellationToken ct)
            {
                if (_load != null)
                {
                    await _load.Task.WaitAsync(ct).ConfigureAwait(false);
                }
                ModifyState(state =>
                {
                    foreach (var item in InitialState)
                    {
                        state[item.Key] = item.Value;
                    }
                });
            }

            private readonly Channel<IDictionary<string, JsonNode?>> _batches =
                Channel.CreateUnbounded<IDictionary<string, JsonNode?>>();
            private readonly TaskCompletionSource? _load;
        }

        // ── IDictionary<string,JsonNode?> interface members ──────────────────

        [Fact]
        public void Keys_ReturnsSnapshotOfCurrentKeys()
        {
            var store = new TestStore
            {
                InitialState = { ["a"] = JsonValue.Create(1), ["b"] = JsonValue.Create(2) }
            };
            store.Start();

            var keys = store.Keys;

            Assert.Equal(2, keys.Count);
            Assert.Contains("a", keys);
            Assert.Contains("b", keys);
        }

        [Fact]
        public void Values_ReturnsSnapshotOfCurrentValues()
        {
            var store = new TestStore
            {
                InitialState = { ["x"] = JsonValue.Create(99) }
            };
            store.Start();

            var values = store.Values;

            Assert.Single(values);
        }

        [Fact]
        public void Count_ReturnsNumberOfEntries()
        {
            var store = new TestStore
            {
                InitialState =
                {
                    ["one"] = JsonValue.Create(1),
                    ["two"] = JsonValue.Create(2),
                    ["three"] = JsonValue.Create(3)
                }
            };
            store.Start();

            Assert.Equal(3, store.Count);
        }

        [Fact]
        public void IsReadOnly_IsFalse()
        {
            var store = new TestStore();
            Assert.False(store.IsReadOnly);
        }

        [Fact]
        public async Task Add_StringValue_AddsToStateAndSynchronizesAsync()
        {
            await using var store = new TestStore();
            store.Start();
            await store;

            store.Add("newKey", JsonValue.Create("hello"));

            Assert.True(store.TryGetValue("newKey", out var value));
            Assert.Equal("\"hello\"", value!.ToJsonString());
        }

        [Fact]
        public async Task Add_KeyValuePair_AddsToStateAsync()
        {
            await using var store = new TestStore();
            store.Start();
            await store;

            ((System.Collections.Generic.IDictionary<string, JsonNode?>)store)
                .Add(new System.Collections.Generic.KeyValuePair<string, JsonNode?>(
                    "kp", JsonValue.Create(7)));

            Assert.True(store.TryGetValue("kp", out var value));
            Assert.Equal("7", value!.ToJsonString());
        }

        [Fact]
        public void Contains_MatchingItem_ReturnsTrue()
        {
            var node = JsonValue.Create(42);
            var store = new TestStore { InitialState = { ["k"] = node } };
            store.Start();

            var found = store.Contains(
                new System.Collections.Generic.KeyValuePair<string, JsonNode?>("k", node));

            Assert.True(found);
        }

        [Fact]
        public void Contains_DifferentValue_ReturnsFalse()
        {
            var store = new TestStore { InitialState = { ["k"] = JsonValue.Create(1) } };
            store.Start();

            var found = store.Contains(
                new System.Collections.Generic.KeyValuePair<string, JsonNode?>(
                    "k", JsonValue.Create(999)));

            Assert.False(found);
        }

        [Fact]
        public void CopyTo_CopiesToArray()
        {
            var store = new TestStore { InitialState = { ["a"] = JsonValue.Create(1) } };
            store.Start();

            var array = new System.Collections.Generic.KeyValuePair<string, JsonNode?>[2];
            store.CopyTo(array, 0);

            Assert.Equal("a", array[0].Key);
        }

        [Fact]
        public void GetEnumerator_EnumeratesEntries()
        {
            var store = new TestStore { InitialState = { ["e"] = JsonValue.Create(5) } };
            store.Start();

            var count = 0;
            foreach (var _ in store)
            {
                count++;
            }

            Assert.Equal(1, count);
        }

        [Fact]
        public async Task Remove_KeyValuePair_MatchingValue_RemovesEntryAsync()
        {
            var node = JsonValue.Create("match");
            await using var store = new TestStore { InitialState = { ["k"] = node } };
            store.Start();
            await store;

            var removed = store.Remove(
                new System.Collections.Generic.KeyValuePair<string, JsonNode?>("k", node));

            Assert.True(removed);
            Assert.False(store.ContainsKey("k"));
        }

        [Fact]
        public void Dispose_SynchronousPath_DoesNotThrow()
        {
            var store = new TestStore();
            store.Start();
            // IDisposable.Dispose() is also available — must not throw.
            ((IDisposable)store).Dispose();
        }
    }
}
