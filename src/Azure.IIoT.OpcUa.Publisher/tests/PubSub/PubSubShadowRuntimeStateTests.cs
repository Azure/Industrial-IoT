// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.PubSub
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.PubSub;
    using Moq;
    using Opc.Ua.PubSub.Diagnostics;
    using System;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="PubSubShadowRuntimeStateProvider"/> and
    /// <see cref="PubSubShadowDiagnosticsBridge"/>.
    /// </summary>
    public sealed class PubSubShadowRuntimeStateTests
    {
        // ── PubSubShadowRuntimeStateProvider ─────────────────────────────────

        [Fact]
        public void DefaultState_AllZeroAndNotRunning()
        {
            var provider = new PubSubShadowRuntimeStateProvider();

            var state = provider.State;

            Assert.False(state.IsRunning);
            Assert.Equal(0, state.StartCount);
            Assert.Equal(0, state.StopCount);
            Assert.Equal(0, state.ConfigurationGeneration);
            Assert.Equal(0, state.WriterGroupCount);
            Assert.Equal(0, state.DataSetWriterCount);
            Assert.Equal(0, state.CaptureCount);
            Assert.Null(state.LastError);
        }

        [Fact]
        public void Started_SetsIsRunningAndIncrementsStartCount()
        {
            var provider = new PubSubShadowRuntimeStateProvider();

            provider.Started();

            var state = provider.State;
            Assert.True(state.IsRunning);
            Assert.Equal(1, state.StartCount);
            Assert.Equal(0, state.StopCount);
        }

        [Fact]
        public void Stopped_SetsIsRunningFalseAndIncrementsStopCount()
        {
            var provider = new PubSubShadowRuntimeStateProvider();
            provider.Started();

            provider.Stopped();

            var state = provider.State;
            Assert.False(state.IsRunning);
            Assert.Equal(1, state.StopCount);
        }

        [Fact]
        public void StartedStopped_Twice_CountsAccumulate()
        {
            var provider = new PubSubShadowRuntimeStateProvider();

            provider.Started();
            provider.Stopped();
            provider.Started();
            provider.Stopped();

            var state = provider.State;
            Assert.False(state.IsRunning);
            Assert.Equal(2, state.StartCount);
            Assert.Equal(2, state.StopCount);
        }

        [Fact]
        public void Replaced_IncrementsConfigurationGenerationAndSetsGroupCounts()
        {
            var provider = new PubSubShadowRuntimeStateProvider();

            provider.Replaced(writerGroupCount: 3, dataSetWriterCount: 7);

            var state = provider.State;
            Assert.Equal(1, state.ConfigurationGeneration);
            Assert.Equal(3, state.WriterGroupCount);
            Assert.Equal(7, state.DataSetWriterCount);
        }

        [Fact]
        public void Replaced_CalledTwice_GenerationIncrementsEachTime()
        {
            var provider = new PubSubShadowRuntimeStateProvider();

            provider.Replaced(1, 2);
            provider.Replaced(4, 8);

            var state = provider.State;
            Assert.Equal(2, state.ConfigurationGeneration);
            Assert.Equal(4, state.WriterGroupCount);
            Assert.Equal(8, state.DataSetWriterCount);
        }

        [Fact]
        public void Replaced_ClearsLastError()
        {
            var provider = new PubSubShadowRuntimeStateProvider();
            provider.Failed(new InvalidOperationException("old error"));

            provider.Replaced(0, 0);

            Assert.Null(provider.State.LastError);
        }

        [Fact]
        public void Captured_IncrementsCaptureCount()
        {
            var provider = new PubSubShadowRuntimeStateProvider();

            provider.Captured();
            provider.Captured();
            provider.Captured();

            Assert.Equal(3, provider.State.CaptureCount);
        }

        [Fact]
        public void Failed_SetsLastErrorToExceptionMessage()
        {
            var provider = new PubSubShadowRuntimeStateProvider();

            provider.Failed(new InvalidOperationException("something went wrong"));

            Assert.Equal("something went wrong", provider.State.LastError);
        }

        [Fact]
        public void Failed_Twice_OverwritesPreviousError()
        {
            var provider = new PubSubShadowRuntimeStateProvider();
            provider.Failed(new InvalidOperationException("first error"));

            provider.Failed(new InvalidOperationException("second error"));

            Assert.Equal("second error", provider.State.LastError);
        }

        [Fact]
        public void Failed_NullException_ThrowsArgumentNullException()
        {
            var provider = new PubSubShadowRuntimeStateProvider();

            Assert.Throws<ArgumentNullException>(() => provider.Failed(null!));
        }

        [Fact]
        public void State_ReturnsConsistentSnapshot()
        {
            var provider = new PubSubShadowRuntimeStateProvider();
            provider.Started();
            provider.Replaced(2, 5);
            provider.Captured();
            provider.Captured();

            var state = provider.State;

            Assert.True(state.IsRunning);
            Assert.Equal(1, state.StartCount);
            Assert.Equal(0, state.StopCount);
            Assert.Equal(1, state.ConfigurationGeneration);
            Assert.Equal(2, state.WriterGroupCount);
            Assert.Equal(5, state.DataSetWriterCount);
            Assert.Equal(2, state.CaptureCount);
            Assert.Null(state.LastError);
        }

        // ── PubSubShadowDiagnosticsBridge ─────────────────────────────────────

        [Fact]
        public void Apply_NullDiagnostic_ThrowsArgumentNullException()
        {
            var nativeDiag = new Mock<IPubSubDiagnostics>();
            var egress = new FakeEgressMetricsProvider();

            Assert.Throws<ArgumentNullException>(() =>
                PubSubShadowDiagnosticsBridge.Apply(null!, nativeDiag.Object, egress));
        }

        [Fact]
        public void Apply_NullNativeDiagnostics_ThrowsArgumentNullException()
        {
            var diagnostic = new WriterGroupDiagnosticModel();
            var egress = new FakeEgressMetricsProvider();

            Assert.Throws<ArgumentNullException>(() =>
                PubSubShadowDiagnosticsBridge.Apply(diagnostic, null!, egress));
        }

        [Fact]
        public void Apply_NullEgress_ThrowsArgumentNullException()
        {
            var diagnostic = new WriterGroupDiagnosticModel();
            var nativeDiag = new Mock<IPubSubDiagnostics>();

            Assert.Throws<ArgumentNullException>(() =>
                PubSubShadowDiagnosticsBridge.Apply(diagnostic, nativeDiag.Object, null!));
        }

        [Fact]
        public void Apply_MapsEgressMetricsToWriterGroupDiagnostic()
        {
            var diagnostic = new WriterGroupDiagnosticModel { WriterGroupName = "wg1" };
            var nativeDiag = new Mock<IPubSubDiagnostics>();
            nativeDiag.Setup(d => d.Read(PubSubDiagnosticsCounterKind.SentNetworkMessages))
                .Returns(42);
            nativeDiag.Setup(d => d.Read(PubSubDiagnosticsCounterKind.SentDataSetMessages))
                .Returns(99);
            var egress = new FakeEgressMetricsProvider
            {
                Metrics = new PubSubShadowEgressMetrics
                {
                    QueueDepth = 5,
                    BackpressureCount = 0,
                    OverflowCount = 2,
                    RetryCount = 3,
                    SentCount = 100,
                    FailedCount = 7,
                    ChunkCount = 0
                }
            };

            var result = PubSubShadowDiagnosticsBridge.Apply(diagnostic, nativeDiag.Object, egress);

            Assert.Equal("wg1", result.WriterGroupName);             // unchanged field preserved
            Assert.Equal(5, result.OutgressInputBufferCount);
            Assert.Equal(2, result.OutgressInputBufferDropped);
            Assert.Equal(100, result.OutgressIoTMessageCount);
            Assert.Equal(7, result.OutgressIoTMessageFailedCount);
            Assert.Equal(3, result.ConnectionRetries);
            Assert.Equal(42, result.EncoderIoTMessagesProcessed);
            Assert.Equal(99, result.EncoderNotificationsProcessed);
        }

        [Fact]
        public void Apply_DoesNotMutateOriginalDiagnostic()
        {
            var original = new WriterGroupDiagnosticModel { OutgressIoTMessageCount = 77 };
            var nativeDiag = new Mock<IPubSubDiagnostics>();
            var egress = new FakeEgressMetricsProvider
            {
                Metrics = new PubSubShadowEgressMetrics
                {
                    QueueDepth = 0,
                    BackpressureCount = 0,
                    OverflowCount = 0,
                    RetryCount = 0,
                    SentCount = 999,
                    FailedCount = 0,
                    ChunkCount = 0
                }
            };

            var result = PubSubShadowDiagnosticsBridge.Apply(original, nativeDiag.Object, egress);

            // `with` expression creates a new record; original is unchanged
            Assert.Equal(77, original.OutgressIoTMessageCount);
            Assert.Equal(999, result.OutgressIoTMessageCount);
            Assert.NotSame(original, result);
        }

        // ── helpers ──────────────────────────────────────────────────────────

        private sealed class FakeEgressMetricsProvider : IPubSubShadowEgressMetricsProvider
        {
            public PubSubShadowEgressMetrics Metrics { get; set; } = new PubSubShadowEgressMetrics
            {
                QueueDepth = 0,
                BackpressureCount = 0,
                OverflowCount = 0,
                RetryCount = 0,
                SentCount = 0,
                FailedCount = 0,
                ChunkCount = 0
            };
        }
    }
}
