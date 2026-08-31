// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.PubSub
{
    using Moq;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Unit tests for <see cref="PubSubIdentityRegistry.ValidateSnapshot"/>.
    /// These exercise the pure static validation logic — no async I/O or store.
    /// </summary>
    public sealed class PubSubIdentityRegistryTests
    {
        // ── ValidateSnapshot — null / empty ───────────────────────────────────

        [Fact]
        public void ValidateSnapshot_NullSnapshot_ThrowsInvalidDataException()
        {
            Assert.Throws<InvalidDataException>(() =>
                PubSubIdentityRegistry.ValidateSnapshot(null));
        }

        [Fact]
        public void ValidateSnapshot_NullEntries_ThrowsInvalidDataException()
        {
            var snapshot = new PubSubIdentityRegistrySnapshot { Entries = null! };
            Assert.Throws<InvalidDataException>(() =>
                PubSubIdentityRegistry.ValidateSnapshot(snapshot));
        }

        [Fact]
        public void ValidateSnapshot_EmptyEntries_ReturnsEmptyDictionaries()
        {
            var snapshot = new PubSubIdentityRegistrySnapshot { Entries = [] };

            var (entries, reverse) = PubSubIdentityRegistry.ValidateSnapshot(snapshot);

            Assert.Empty(entries);
            Assert.Empty(reverse);
        }

        // ── ValidateSnapshot — valid entry ────────────────────────────────────

        [Fact]
        public void ValidateSnapshot_SingleValidEntry_PopulatesEntries()
        {
            var snapshot = new PubSubIdentityRegistrySnapshot
            {
                Entries =
                [
                    new PubSubIdentityRegistryEntry
                    {
                        Scope = "writer-group",
                        Id = "my-group",
                        Value = 1
                    }
                ]
            };

            var (entries, reverse) = PubSubIdentityRegistry.ValidateSnapshot(snapshot);

            Assert.Single(entries);
            Assert.Single(reverse);
        }

        [Fact]
        public void ValidateSnapshot_MultipleValidEntries_AllPopulated()
        {
            var snapshot = new PubSubIdentityRegistrySnapshot
            {
                Entries =
                [
                    new PubSubIdentityRegistryEntry
                    {
                        Scope = "writer-group",
                        Id = "group-1",
                        Value = 1
                    },
                    new PubSubIdentityRegistryEntry
                    {
                        Scope = "data-set-writer",
                        Id = "writer-1",
                        Value = 2
                    }
                ]
            };

            var (entries, reverse) = PubSubIdentityRegistry.ValidateSnapshot(snapshot);

            Assert.Equal(2, entries.Count);
            Assert.Equal(2, reverse.Count);
        }

        // ── ValidateSnapshot — invalid entries ────────────────────────────────

        [Fact]
        public void ValidateSnapshot_NullEntryInList_ThrowsInvalidDataException()
        {
            var snapshot = new PubSubIdentityRegistrySnapshot
            {
                Entries = [null!]
            };

            Assert.Throws<InvalidDataException>(() =>
                PubSubIdentityRegistry.ValidateSnapshot(snapshot));
        }

        [Fact]
        public void ValidateSnapshot_EntryWithZeroValue_ThrowsInvalidDataException()
        {
            var snapshot = new PubSubIdentityRegistrySnapshot
            {
                Entries =
                [
                    new PubSubIdentityRegistryEntry
                    {
                        Scope = "writer-group",
                        Id = "group-1",
                        Value = 0     // zero is not a valid PubSub identifier
                    }
                ]
            };

            Assert.Throws<InvalidDataException>(() =>
                PubSubIdentityRegistry.ValidateSnapshot(snapshot));
        }

        [Fact]
        public void ValidateSnapshot_EntryWithEmptyScope_ThrowsInvalidDataException()
        {
            var snapshot = new PubSubIdentityRegistrySnapshot
            {
                Entries =
                [
                    new PubSubIdentityRegistryEntry
                    {
                        Scope = "",
                        Id = "group-1",
                        Value = 1
                    }
                ]
            };

            Assert.Throws<InvalidDataException>(() =>
                PubSubIdentityRegistry.ValidateSnapshot(snapshot));
        }

        [Fact]
        public void ValidateSnapshot_EntryWithEmptyId_ThrowsInvalidDataException()
        {
            var snapshot = new PubSubIdentityRegistrySnapshot
            {
                Entries =
                [
                    new PubSubIdentityRegistryEntry
                    {
                        Scope = "writer-group",
                        Id = "",
                        Value = 1
                    }
                ]
            };

            Assert.Throws<InvalidDataException>(() =>
                PubSubIdentityRegistry.ValidateSnapshot(snapshot));
        }

        [Fact]
        public void ValidateSnapshot_DuplicateScopeIdCombination_ThrowsInvalidDataException()
        {
            var snapshot = new PubSubIdentityRegistrySnapshot
            {
                Entries =
                [
                    new PubSubIdentityRegistryEntry
                    {
                        Scope = "writer-group",
                        Id = "group-1",
                        Value = 1
                    },
                    new PubSubIdentityRegistryEntry
                    {
                        Scope = "writer-group",
                        Id = "group-1",   // same scope + id as above
                        Value = 2
                    }
                ]
            };

            Assert.Throws<InvalidDataException>(() =>
                PubSubIdentityRegistry.ValidateSnapshot(snapshot));
        }

        [Fact]
        public void ValidateSnapshot_DuplicateNativeIdWithinScope_ThrowsInvalidDataException()
        {
            var snapshot = new PubSubIdentityRegistrySnapshot
            {
                Entries =
                [
                    new PubSubIdentityRegistryEntry
                    {
                        Scope = "writer-group",
                        Id = "group-1",
                        Value = 1
                    },
                    new PubSubIdentityRegistryEntry
                    {
                        Scope = "writer-group",
                        Id = "group-2",
                        Value = 1   // same native id in same scope
                    }
                ]
            };

            Assert.Throws<InvalidDataException>(() =>
                PubSubIdentityRegistry.ValidateSnapshot(snapshot));
        }

        [Fact]
        public void ValidateSnapshot_SameScopeNativeIdInDifferentScopes_IsAllowed()
        {
            // The same native ushort can appear in different scopes since
            // native ids are scoped — writer-group 1 and data-set-writer 1
            // are distinct.
            var snapshot = new PubSubIdentityRegistrySnapshot
            {
                Entries =
                [
                    new PubSubIdentityRegistryEntry
                    {
                        Scope = "writer-group",
                        Id = "group-1",
                        Value = 1
                    },
                    new PubSubIdentityRegistryEntry
                    {
                        Scope = "data-set-writer",
                        Id = "writer-1",
                        Value = 1   // same native id but different scope — OK
                    }
                ]
            };

            var (entries, reverse) = PubSubIdentityRegistry.ValidateSnapshot(snapshot);

            Assert.Equal(2, entries.Count);
            Assert.Equal(2, reverse.Count);
        }

        [Fact]
        public void ValidateSnapshot_EntryWithControlSeparatorInScope_ThrowsInvalidDataException()
        {
            var snapshot = new PubSubIdentityRegistrySnapshot
            {
                Entries =
                [
                    new PubSubIdentityRegistryEntry
                    {
                        Scope = "writer\u001fgroup",   // contains unit separator
                        Id = "group-1",
                        Value = 1
                    }
                ]
            };

            Assert.Throws<InvalidDataException>(() =>
                PubSubIdentityRegistry.ValidateSnapshot(snapshot));
        }

        // ── PubSubIdentityRegistry full transaction tests ─────────────────────

        [Fact]
        public async Task GetOrAllocate_NewEntry_AssignsNonZeroId()
        {
            var store = CreateEmptyStore();
            var registry = new PubSubIdentityRegistry(store);

            await using var tx = await registry.BeginAsync();
            var id = tx.GetOrAllocate("writer-group", "group-1");

            Assert.NotEqual(0, id);
        }

        [Fact]
        public async Task GetOrAllocate_SameKeyTwice_ReturnsSameId()
        {
            var store = CreateEmptyStore();
            var registry = new PubSubIdentityRegistry(store);

            await using var tx = await registry.BeginAsync();
            var id1 = tx.GetOrAllocate("writer-group", "group-1");
            var id2 = tx.GetOrAllocate("writer-group", "group-1");

            Assert.Equal(id1, id2);
        }

        [Fact]
        public async Task GetOrAllocate_DifferentKeys_ReturnsDifferentIds()
        {
            var store = CreateEmptyStore();
            var registry = new PubSubIdentityRegistry(store);

            await using var tx = await registry.BeginAsync();
            var id1 = tx.GetOrAllocate("writer-group", "group-1");
            var id2 = tx.GetOrAllocate("writer-group", "group-2");

            Assert.NotEqual(id1, id2);
        }

        [Fact]
        public async Task TryGetIdAsync_AfterCommit_ReturnsAssignedId()
        {
            var store = CreateEmptyStore();
            var registry = new PubSubIdentityRegistry(store);

            ushort allocated;
            await using (var tx = await registry.BeginAsync())
            {
                allocated = tx.GetOrAllocate("writer-group", "group-1");
                await tx.CommitAsync();
            }

            var found = await registry.TryGetIdAsync("writer-group", "group-1");

            Assert.Equal(allocated, found);
        }

        [Fact]
        public async Task TryGetIdAsync_BeforeCommit_ReturnsNull()
        {
            var store = CreateEmptyStore();
            var registry = new PubSubIdentityRegistry(store);

            // Initialize the registry without committing anything
            await using (var tx = await registry.BeginAsync())
            {
                _ = tx.GetOrAllocate("writer-group", "group-1");
                // No commit — discard
            }

            var found = await registry.TryGetIdAsync("writer-group", "group-1");

            Assert.Null(found);
        }

        [Fact]
        public async Task TryGetPublicIdAsync_AfterCommit_ReturnsPublicId()
        {
            var store = CreateEmptyStore();
            var registry = new PubSubIdentityRegistry(store);

            ushort allocated;
            await using (var tx = await registry.BeginAsync())
            {
                allocated = tx.GetOrAllocate("writer-group", "group-1");
                await tx.CommitAsync();
            }

            var publicId = await registry.TryGetPublicIdAsync("writer-group", allocated);

            Assert.Equal("group-1", publicId);
        }

        [Fact]
        public async Task TryGetPublicIdAsync_UnknownId_ReturnsNull()
        {
            var store = CreateEmptyStore();
            var registry = new PubSubIdentityRegistry(store);

            // Initialize with a commit
            await using (var tx = await registry.BeginAsync())
            {
                _ = tx.GetOrAllocate("writer-group", "group-1");
                await tx.CommitAsync();
            }

            var publicId = await registry.TryGetPublicIdAsync("writer-group", 9999);

            Assert.Null(publicId);
        }

        [Fact]
        public async Task Registry_LoadsExistingEntriesFromStore()
        {
            var snapshot = new PubSubIdentityRegistrySnapshot
            {
                Entries =
                [
                    new PubSubIdentityRegistryEntry
                    {
                        Scope = "writer-group",
                        Id = "group-1",
                        Value = 42
                    }
                ]
            };
            var store = CreateStoreWithSnapshot(snapshot);
            var registry = new PubSubIdentityRegistry(store);

            var found = await registry.TryGetIdAsync("writer-group", "group-1");

            Assert.Equal((ushort)42, found);
        }

        [Fact]
        public async Task GetOrAllocate_ExistingEntryInStore_ReturnsStoredId()
        {
            var snapshot = new PubSubIdentityRegistrySnapshot
            {
                Entries =
                [
                    new PubSubIdentityRegistryEntry
                    {
                        Scope = "writer-group",
                        Id = "group-1",
                        Value = 42
                    }
                ]
            };
            var store = CreateStoreWithSnapshot(snapshot);
            var registry = new PubSubIdentityRegistry(store);

            await using var tx = await registry.BeginAsync();
            var id = tx.GetOrAllocate("writer-group", "group-1");

            Assert.Equal((ushort)42, id);
        }

        private static IPubSubIdentityRegistryStore CreateEmptyStore()
        {
            var mock = new Mock<IPubSubIdentityRegistryStore>();
            var saved = new PubSubIdentityRegistrySnapshot { Entries = [] };
            mock.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => saved);
            mock.Setup(s => s.SaveAsync(It.IsAny<PubSubIdentityRegistrySnapshot>(),
                    It.IsAny<CancellationToken>()))
                .Callback<PubSubIdentityRegistrySnapshot, CancellationToken>(
                    (snap, _) => saved = snap)
                .Returns(ValueTask.CompletedTask);
            return mock.Object;
        }

        private static IPubSubIdentityRegistryStore CreateStoreWithSnapshot(
            PubSubIdentityRegistrySnapshot snapshot)
        {
            var mock = new Mock<IPubSubIdentityRegistryStore>();
            mock.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(snapshot);
            mock.Setup(s => s.SaveAsync(It.IsAny<PubSubIdentityRegistrySnapshot>(),
                    It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);
            return mock.Object;
        }
    }
}
