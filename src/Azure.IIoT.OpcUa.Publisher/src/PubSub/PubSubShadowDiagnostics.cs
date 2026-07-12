// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.PubSub
{
    using Opc.Ua;
    using Opc.Ua.PubSub.Diagnostics;
    using Opc.Ua.PubSub.Encoding;
    using Opc.Ua.PubSub.Encoding.Json;
    using Opc.Ua.PubSub.Encoding.Uadp;
    using Opc.Ua.PubSub.MetaData;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Standard PubSub encoding used by a shadow capture.
    /// </summary>
    public enum PubSubShadowEncoding
    {
        /// <summary>
        /// OPC UA JSON PubSub encoding.
        /// </summary>
        Json,

        /// <summary>
        /// OPC UA UADP PubSub encoding.
        /// </summary>
        Uadp
    }

    /// <summary>
    /// A deep-copied encoded shadow frame. A capture is diagnostic data only
    /// and is never sent to an event client.
    /// </summary>
    public sealed class PubSubShadowCapture
    {
        /// <summary>
        /// Initializes a capture with an owned copy of <paramref name="payload"/>.
        /// </summary>
        /// <param name="encoding">Encoding used for the frame.</param>
        /// <param name="capturedAt">Capture timestamp.</param>
        /// <param name="payload">Encoded payload to copy.</param>
        public PubSubShadowCapture(PubSubShadowEncoding encoding,
            DateTimeOffset capturedAt, ReadOnlySpan<byte> payload)
        {
            Encoding = encoding;
            CapturedAt = capturedAt;
            _payload = payload.ToArray();
        }

        /// <summary>
        /// Gets the encoding used to create the frame.
        /// </summary>
        public PubSubShadowEncoding Encoding { get; }

        /// <summary>
        /// Gets the capture timestamp.
        /// </summary>
        public DateTimeOffset CapturedAt { get; }

        /// <summary>
        /// Gets the owned encoded payload. Consumers must treat it as immutable.
        /// </summary>
        public ReadOnlyMemory<byte> Payload => _payload;

        /// <summary>
        /// Creates a capture with its own payload copy.
        /// </summary>
        /// <returns>A deep copy of the capture.</returns>
        public PubSubShadowCapture Clone()
        {
            return new PubSubShadowCapture(Encoding, CapturedAt, _payload);
        }

        private readonly byte[] _payload;
    }

    /// <summary>
    /// Receives shadow-encoded PubSub frames. Implementations must not
    /// dispatch captured data to an <c>IEventClient</c>.
    /// </summary>
    public interface IPubSubShadowCaptureSink
    {
        /// <summary>
        /// Captures a deep-copied frame.
        /// </summary>
        /// <param name="capture">Frame to capture.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that completes once the frame is captured.</returns>
        ValueTask CaptureAsync(PubSubShadowCapture capture,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Exposes captured frames to test diagnostics without providing an
    /// egress path.
    /// </summary>
    public interface IPubSubShadowCaptureStore
    {
        /// <summary>
        /// Gets a snapshot of captured frames.
        /// </summary>
        IReadOnlyList<PubSubShadowCapture> Captures { get; }
    }

    /// <summary>
    /// In-memory capture sink used by the inert shadow host.
    /// </summary>
    public sealed class InMemoryPubSubShadowCaptureSink :
        IPubSubShadowCaptureSink, IPubSubShadowCaptureStore
    {
        /// <inheritdoc/>
        public IReadOnlyList<PubSubShadowCapture> Captures
        {
            get
            {
                lock (_gate)
                {
                    return _captures.ConvertAll(capture => capture.Clone()).AsReadOnly();
                }
            }
        }

        /// <inheritdoc/>
        public ValueTask CaptureAsync(PubSubShadowCapture capture,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(capture);
            cancellationToken.ThrowIfCancellationRequested();
            lock (_gate)
            {
                _captures.Add(capture.Clone());
            }
            return default;
        }

        private readonly Lock _gate = new();
        private readonly List<PubSubShadowCapture> _captures = [];
    }

    /// <summary>
    /// Snapshot of the inert standard PubSub runtime.
    /// </summary>
    public sealed record class PubSubShadowRuntimeState
    {
        /// <summary>
        /// Gets whether the standard PubSub application is running.
        /// </summary>
        public required bool IsRunning { get; init; }

        /// <summary>
        /// Gets the number of successful host starts.
        /// </summary>
        public required long StartCount { get; init; }

        /// <summary>
        /// Gets the number of successful host stops.
        /// </summary>
        public required long StopCount { get; init; }

        /// <summary>
        /// Gets the number of committed configuration replacements.
        /// </summary>
        public required long ConfigurationGeneration { get; init; }

        /// <summary>
        /// Gets the number of translated writer groups in the active
        /// shadow configuration.
        /// </summary>
        public required int WriterGroupCount { get; init; }

        /// <summary>
        /// Gets the number of translated dataset writers in the active
        /// shadow configuration.
        /// </summary>
        public required int DataSetWriterCount { get; init; }

        /// <summary>
        /// Gets the number of shadow frames captured.
        /// </summary>
        public required long CaptureCount { get; init; }

        /// <summary>
        /// Gets the latest configuration or capture error, if any.
        /// </summary>
        public string? LastError { get; init; }
    }

    /// <summary>
    /// Provides inert PubSub host and shadow diagnostics state without
    /// exposing native OPC UA PubSub runtime types.
    /// </summary>
    public interface IPubSubShadowRuntimeStateProvider
    {
        /// <summary>
        /// Gets a consistent runtime state snapshot.
        /// </summary>
        PubSubShadowRuntimeState State { get; }
    }

    internal sealed class PubSubShadowRuntimeStateProvider :
        IPubSubShadowRuntimeStateProvider
    {
        public PubSubShadowRuntimeState State
        {
            get
            {
                lock (_gate)
                {
                    return new PubSubShadowRuntimeState
                    {
                        IsRunning = _isRunning,
                        StartCount = _startCount,
                        StopCount = _stopCount,
                        ConfigurationGeneration = _configurationGeneration,
                        WriterGroupCount = _writerGroupCount,
                        DataSetWriterCount = _dataSetWriterCount,
                        CaptureCount = _captureCount,
                        LastError = _lastError
                    };
                }
            }
        }

        public void Started()
        {
            lock (_gate)
            {
                _isRunning = true;
                _startCount++;
            }
        }

        public void Stopped()
        {
            lock (_gate)
            {
                _isRunning = false;
                _stopCount++;
            }
        }

        public void Replaced(int writerGroupCount, int dataSetWriterCount)
        {
            lock (_gate)
            {
                _configurationGeneration++;
                _writerGroupCount = writerGroupCount;
                _dataSetWriterCount = dataSetWriterCount;
                _lastError = null;
            }
        }

        public void Captured()
        {
            lock (_gate)
            {
                _captureCount++;
            }
        }

        public void Failed(Exception exception)
        {
            ArgumentNullException.ThrowIfNull(exception);
            lock (_gate)
            {
                _lastError = exception.Message;
            }
        }

        private readonly Lock _gate = new();
        private bool _isRunning;
        private long _startCount;
        private long _stopCount;
        private long _configurationGeneration;
        private int _writerGroupCount;
        private int _dataSetWriterCount;
        private long _captureCount;
        private string? _lastError;
    }

    internal sealed class PubSubShadowEncodingBridge
    {
        public PubSubShadowEncodingBridge(IPubSubShadowCaptureSink captureSink,
            PubSubShadowRuntimeStateProvider state, TimeProvider? timeProvider = null)
        {
            _captureSink = captureSink ?? throw new ArgumentNullException(nameof(captureSink));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        public async ValueTask CaptureJsonAsync(PubSubNetworkMessage message,
            CancellationToken cancellationToken = default)
        {
            var encoded = await new Opc.Ua.PubSub.Encoding.Json.JsonEncoder().EncodeAsync(message, CreateContext(),
                cancellationToken).ConfigureAwait(false);
            await CaptureAsync(PubSubShadowEncoding.Json, encoded, cancellationToken)
                .ConfigureAwait(false);
        }

        public async ValueTask CaptureUadpAsync(PubSubNetworkMessage message,
            CancellationToken cancellationToken = default)
        {
            var encoded = await new UadpEncoder().EncodeAsync(message, CreateContext(),
                cancellationToken).ConfigureAwait(false);
            await CaptureAsync(PubSubShadowEncoding.Uadp, encoded, cancellationToken)
                .ConfigureAwait(false);
        }

        private async ValueTask CaptureAsync(PubSubShadowEncoding encoding,
            ReadOnlyMemory<byte> encoded, CancellationToken cancellationToken)
        {
            try
            {
                await _captureSink.CaptureAsync(new PubSubShadowCapture(encoding,
                    _timeProvider.GetUtcNow(), encoded.Span), cancellationToken)
                    .ConfigureAwait(false);
                _state.Captured();
            }
            catch (Exception exception)
            {
                _state.Failed(exception);
                throw;
            }
        }

        private PubSubNetworkMessageContext CreateContext()
        {
            return new PubSubNetworkMessageContext(
                ServiceMessageContext.CreateEmpty(null!),
                new DataSetMetaDataRegistry(),
                new PubSubDiagnostics(PubSubDiagnosticsLevel.Low),
                _timeProvider);
        }

        private readonly IPubSubShadowCaptureSink _captureSink;
        private readonly PubSubShadowRuntimeStateProvider _state;
        private readonly TimeProvider _timeProvider;
    }
}
