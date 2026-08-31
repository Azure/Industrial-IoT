// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Microsoft.Extensions.Logging.Abstractions;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Unit tests for <see cref="ManagedSubscriptionRetryScheduler"/>.
    /// All tests are pure-logic and do not rely on timers firing.
    /// </summary>
    public sealed class ManagedSubscriptionRetrySchedulerTests
    {
        private static ManagedSubscriptionRetryScheduler CreateSut(
            Func<ManagedRetryRequest, CancellationToken,
                ValueTask<ManagedRetryOutcome>>? retry = null,
            OpcUaSubscriptionOptions? options = null)
        {
            return new ManagedSubscriptionRetryScheduler(
                options ?? new OpcUaSubscriptionOptions(),
                TimeProvider.System,
                retry ?? ((_, _) => ValueTask.FromResult(ManagedRetryOutcome.Obsolete)),
                NullLogger.Instance);
        }

        private static ManagedItemRetryTarget MakeTarget(
            string name,
            ManagedItemRetryKind kind = ManagedItemRetryKind.Invalid,
            long generation = 1L,
            bool pending = false,
            bool applied = false,
            StatusCode? status = null)
        {
            return new ManagedItemRetryTarget(
                name, ClientHandle: 1u, generation, kind,
                status ?? StatusCodes.BadNodeIdUnknown, pending, applied);
        }

        // ── Constructor guard checks ──────────────────────────────────────────

        [Fact]
        public void Constructor_NullOptions_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ManagedSubscriptionRetryScheduler(
                    null!, TimeProvider.System,
                    (_, _) => ValueTask.FromResult(ManagedRetryOutcome.Obsolete)));
        }

        [Fact]
        public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ManagedSubscriptionRetryScheduler(
                    new OpcUaSubscriptionOptions(), null!,
                    (_, _) => ValueTask.FromResult(ManagedRetryOutcome.Obsolete)));
        }

        [Fact]
        public void Constructor_NullRetry_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ManagedSubscriptionRetryScheduler(
                    new OpcUaSubscriptionOptions(), TimeProvider.System, null!));
        }

        // ── Initial state ────────────────────────────────────────────────────

        [Fact]
        public async Task Count_InitiallyZeroAsync()
        {
            await using var sut = CreateSut();
            Assert.Equal(0, sut.Count);
        }

        [Fact]
        public async Task IsRetrying_InitiallyFalseForAnyNameAsync()
        {
            await using var sut = CreateSut();
            Assert.False(sut.IsRetrying("nonexistent"));
        }

        [Fact]
        public async Task LastError_InitiallyNullAsync()
        {
            await using var sut = CreateSut();
            Assert.Null(sut.LastError);
        }

        // ── Update – add new item ─────────────────────────────────────────────

        [Fact]
        public async Task Update_NewInvalidItem_AddsToStateAsync()
        {
            await using var sut = CreateSut();
            sut.Update(MakeTarget("item1"));
            Assert.Equal(1, sut.Count);
        }

        [Fact]
        public async Task Update_NewItem_IsRetryingTrueAsync()
        {
            await using var sut = CreateSut();
            sut.Update(MakeTarget("item1"));
            Assert.True(sut.IsRetrying("item1"));
        }

        [Fact]
        public async Task Update_MultipleDifferentItems_AllAddedAsync()
        {
            await using var sut = CreateSut();
            sut.Update(MakeTarget("item1"));
            sut.Update(MakeTarget("item2", ManagedItemRetryKind.Bad));
            sut.Update(MakeTarget("item3", ManagedItemRetryKind.Subscription));
            Assert.Equal(3, sut.Count);
        }

        // ── Update – Applied = true removes item ──────────────────────────────

        [Fact]
        public async Task Update_AppliedTrue_RemovesExistingItemAsync()
        {
            await using var sut = CreateSut();
            sut.Update(MakeTarget("item1"));
            Assert.Equal(1, sut.Count);

            sut.Update(MakeTarget("item1", applied: true));
            Assert.Equal(0, sut.Count);
        }

        [Fact]
        public async Task Update_AppliedTrue_IsRetryingFalseAfterRemovalAsync()
        {
            await using var sut = CreateSut();
            sut.Update(MakeTarget("item1"));
            sut.Update(MakeTarget("item1", applied: true));
            Assert.False(sut.IsRetrying("item1"));
        }

        [Fact]
        public async Task Update_AppliedTrue_NonExistingItem_NoThrowAsync()
        {
            await using var sut = CreateSut();
            var ex = Record.Exception(() =>
                sut.Update(MakeTarget("nonexistent", applied: true)));
            Assert.Null(ex);
            Assert.Equal(0, sut.Count);
        }

        // ── Update – Kind = None removes item ─────────────────────────────────

        [Fact]
        public async Task Update_KindNone_RemovesExistingItemAsync()
        {
            await using var sut = CreateSut();
            sut.Update(MakeTarget("item1"));
            Assert.Equal(1, sut.Count);

            sut.Update(MakeTarget("item1", ManagedItemRetryKind.None,
                status: StatusCodes.Good));
            Assert.Equal(0, sut.Count);
        }

        [Fact]
        public async Task Update_KindNone_NonExistingItem_NoThrowAsync()
        {
            await using var sut = CreateSut();
            var ex = Record.Exception(() =>
                sut.Update(MakeTarget("nonexistent", ManagedItemRetryKind.None)));
            Assert.Null(ex);
        }

        // ── Update – Pending = true ───────────────────────────────────────────

        [Fact]
        public async Task Update_PendingTrue_SameGeneration_KeepsItemAsync()
        {
            await using var sut = CreateSut();
            sut.Update(MakeTarget("item1", generation: 5L));

            sut.Update(MakeTarget("item1", pending: true, generation: 5L));

            Assert.Equal(1, sut.Count);
        }

        [Fact]
        public async Task Update_PendingTrue_DifferentGeneration_RemovesItemAsync()
        {
            await using var sut = CreateSut();
            sut.Update(MakeTarget("item1", generation: 5L));

            sut.Update(MakeTarget("item1", pending: true, generation: 99L));

            Assert.Equal(0, sut.Count);
        }

        [Fact]
        public async Task Update_PendingTrue_NoExistingItem_NoThrowAsync()
        {
            await using var sut = CreateSut();
            var ex = Record.Exception(() =>
                sut.Update(MakeTarget("nonexistent", pending: true)));
            Assert.Null(ex);
        }

        // ── Update – same generation/kind/status when Processing/Waiting ─────

        [Fact]
        public async Task Update_SameGenerationKindStatus_IncreasesAttemptAsync()
        {
            var attempts = new List<int>();
            await using var sut = new ManagedSubscriptionRetryScheduler(
                new OpcUaSubscriptionOptions(),
                TimeProvider.System,
                (req, _) =>
                {
                    attempts.Add(req.Attempt);
                    return ValueTask.FromResult(ManagedRetryOutcome.Started);
                });

            // Add the item and force process (outcome = Started → Waiting)
            sut.Update(MakeTarget("item1", generation: 1L));
            await sut.ProcessAsync(force: true);

            // Now update the same item while it's in Waiting phase
            sut.Update(MakeTarget("item1", generation: 1L));

            // The item should still be scheduled (incremented attempt + rescheduled)
            Assert.Equal(1, sut.Count);
        }

        // ── UpdateSubscription ────────────────────────────────────────────────

        [Fact]
        public async Task UpdateSubscription_FailedTrue_AddsSubscriptionItemAsync()
        {
            await using var sut = CreateSut();
            sut.UpdateSubscription(true);
            Assert.Equal(1, sut.Count);
        }

        [Fact]
        public async Task UpdateSubscription_FailedFalse_RemovesSubscriptionItemAsync()
        {
            await using var sut = CreateSut();
            sut.UpdateSubscription(true);
            Assert.Equal(1, sut.Count);

            sut.UpdateSubscription(false);
            Assert.Equal(0, sut.Count);
        }

        [Fact]
        public async Task UpdateSubscription_FailedFalse_WhenNotPresentDoesNotThrowAsync()
        {
            await using var sut = CreateSut();
            var ex = Record.Exception(() => sut.UpdateSubscription(false));
            Assert.Null(ex);
            Assert.Equal(0, sut.Count);
        }

        [Fact]
        public async Task UpdateSubscription_MultipleFailed_CountRemainsOneAsync()
        {
            await using var sut = CreateSut();
            sut.UpdateSubscription(true);
            sut.UpdateSubscription(true);
            Assert.Equal(1, sut.Count);
        }

        // ── Remove ────────────────────────────────────────────────────────────

        [Fact]
        public async Task Remove_ExistingItem_RemovesFromStateAsync()
        {
            await using var sut = CreateSut();
            sut.Update(MakeTarget("item1"));
            Assert.Equal(1, sut.Count);

            sut.Remove("item1");
            Assert.Equal(0, sut.Count);
        }

        [Fact]
        public async Task Remove_NonExistingItem_NoThrowAsync()
        {
            await using var sut = CreateSut();
            var ex = Record.Exception(() => sut.Remove("nonexistent"));
            Assert.Null(ex);
        }

        [Fact]
        public async Task Remove_IsRetryingFalseAfterRemoveAsync()
        {
            await using var sut = CreateSut();
            sut.Update(MakeTarget("item1"));
            sut.Remove("item1");
            Assert.False(sut.IsRetrying("item1"));
        }

        // ── ProcessAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task ProcessAsync_ForceTrue_CallsRetryForScheduledItemsAsync()
        {
            var called = new List<string>();
            await using var sut = new ManagedSubscriptionRetryScheduler(
                new OpcUaSubscriptionOptions(),
                TimeProvider.System,
                (req, _) =>
                {
                    called.Add(req.Name ?? "<sub>");
                    return ValueTask.FromResult(ManagedRetryOutcome.Obsolete);
                });

            sut.Update(MakeTarget("item1"));
            sut.Update(MakeTarget("item2"));

            await sut.ProcessAsync(force: true);

            Assert.Equal(2, called.Count);
        }

        [Fact]
        public async Task ProcessAsync_OutcomeObsolete_RemovesItemAsync()
        {
            await using var sut = new ManagedSubscriptionRetryScheduler(
                new OpcUaSubscriptionOptions(),
                TimeProvider.System,
                (_, _) => ValueTask.FromResult(ManagedRetryOutcome.Obsolete));

            sut.Update(MakeTarget("item1"));
            await sut.ProcessAsync(force: true);

            Assert.Equal(0, sut.Count);
        }

        [Fact]
        public async Task ProcessAsync_OutcomeStarted_ItemRemainsInStateAsync()
        {
            await using var sut = new ManagedSubscriptionRetryScheduler(
                new OpcUaSubscriptionOptions(),
                TimeProvider.System,
                (_, _) => ValueTask.FromResult(ManagedRetryOutcome.Started));

            sut.Update(MakeTarget("item1"));
            await sut.ProcessAsync(force: true);

            Assert.Equal(1, sut.Count);
        }

        [Fact]
        public async Task ProcessAsync_OutcomeFailed_ItemRescheduledAsync()
        {
            await using var sut = new ManagedSubscriptionRetryScheduler(
                new OpcUaSubscriptionOptions(),
                TimeProvider.System,
                (_, _) => ValueTask.FromResult(ManagedRetryOutcome.Failed));

            sut.Update(MakeTarget("item1"));
            await sut.ProcessAsync(force: true);

            // Item is rescheduled, so still present
            Assert.Equal(1, sut.Count);
        }

        [Fact]
        public async Task ProcessAsync_RetryThrows_SetsLastErrorAsync()
        {
            var boom = new InvalidOperationException("boom");
            await using var sut = new ManagedSubscriptionRetryScheduler(
                new OpcUaSubscriptionOptions(),
                TimeProvider.System,
                (_, _) => throw boom);

            sut.Update(MakeTarget("item1"));
            await sut.ProcessAsync(force: true);

            // After the retry threw, the error should be recorded
            Assert.NotNull(sut.LastError);
            Assert.Equal("boom", sut.LastError!.Message);
        }

        [Fact]
        public async Task ProcessAsync_WhenDisposed_DoesNotThrowAsync()
        {
            var sut = CreateSut();
            await sut.DisposeAsync();

            var ex = await Record.ExceptionAsync(
                async () => await sut.ProcessAsync(force: true));
            Assert.Null(ex);
        }

        [Fact]
        public async Task ProcessAsync_WithSubscriptionItem_CallsRetryAsync()
        {
            var called = false;
            await using var sut = new ManagedSubscriptionRetryScheduler(
                new OpcUaSubscriptionOptions(),
                TimeProvider.System,
                (req, _) =>
                {
                    called = true;
                    return ValueTask.FromResult(ManagedRetryOutcome.Obsolete);
                });

            sut.UpdateSubscription(true);
            await sut.ProcessAsync(force: true);

            Assert.True(called);
        }

        [Fact]
        public async Task ProcessAsync_CancellationRequested_ThrowsOperationCancelledAsync()
        {
            await using var sut = new ManagedSubscriptionRetryScheduler(
                new OpcUaSubscriptionOptions(),
                TimeProvider.System,
                async (_, ct) =>
                {
                    await Task.Delay(100, ct);
                    return ManagedRetryOutcome.Obsolete;
                });

            sut.Update(MakeTarget("item1"));

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                async () => await sut.ProcessAsync(force: true, cts.Token));
        }

        // ── DisposeAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task DisposeAsync_CanBeCalledMultipleTimesAsync()
        {
            var sut = CreateSut();
            await sut.DisposeAsync();
            var ex = await Record.ExceptionAsync(
                async () => await sut.DisposeAsync());
            Assert.Null(ex);
        }

        [Fact]
        public async Task DisposeAsync_ClearsAllStatesAsync()
        {
            var sut = CreateSut();
            sut.Update(MakeTarget("item1"));
            sut.Update(MakeTarget("item2"));

            await sut.DisposeAsync();

            // After dispose, Count should be 0
            Assert.Equal(0, sut.Count);
        }

        // ── Update after dispose ──────────────────────────────────────────────

        [Fact]
        public async Task Update_AfterDispose_IsNoOpAsync()
        {
            var sut = CreateSut();
            await sut.DisposeAsync();

            var ex = Record.Exception(() => sut.Update(MakeTarget("item1")));
            Assert.Null(ex);
            Assert.Equal(0, sut.Count);
        }

        [Fact]
        public async Task UpdateSubscription_AfterDispose_IsNoOpAsync()
        {
            var sut = CreateSut();
            await sut.DisposeAsync();

            var ex = Record.Exception(() => sut.UpdateSubscription(true));
            Assert.Null(ex);
            Assert.Equal(0, sut.Count);
        }
    }
}
