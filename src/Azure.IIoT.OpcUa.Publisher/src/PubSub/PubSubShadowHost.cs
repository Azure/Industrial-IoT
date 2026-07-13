// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.PubSub
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;
    using Opc.Ua;
    using Opc.Ua.PubSub.Application;
    using Opc.Ua.PubSub.Encoding;
    using Opc.Ua.PubSub.Encoding.Json;
    using Opc.Ua.PubSub.Transports;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Hosts one application-wide, inert OPC UA PubSub runtime. It accepts
    /// public Publisher models and never exposes native runtime types.
    /// </summary>
    public interface IPubSubShadowHost
    {
        /// <summary>
        /// Replaces the standard runtime configuration from public Publisher
        /// writer group models. Native identifiers are committed only after
        /// the replacement succeeds.
        /// </summary>
        /// <param name="writerGroups">Writer groups to translate.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that completes after the replacement is committed.</returns>
        ValueTask ReplaceConfigurationAsync(IEnumerable<WriterGroupModel> writerGroups,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Registers the isolated shadow PubSub host. Publisher does not call
    /// this extension in production; it is currently activated by tests only.
    /// </summary>
    public static class PubSubShadowServiceCollectionEx
    {
        /// <summary>
        /// Registers one standard PubSub application, its durable identity
        /// registry, capture-only diagnostics bridge, and buffer seams.
        /// </summary>
        /// <param name="services">Service collection to register with.</param>
        /// <returns>The original service collection.</returns>
        public static IServiceCollection AddPubSubShadowHost(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);
            var registered = services.Any(descriptor =>
                descriptor.ServiceType == typeof(IPubSubShadowHost));

            services.AddOptions<PublisherOptions>();
            services.AddOptions<PubSubShadowCaptureOptions>();
            services.AddOptions<ManagedPubSubNotificationBufferOptions>();
            services.TryAddSingleton<IPubSubIdentityRegistryStore,
                FilePubSubIdentityRegistryStore>();
            services.TryAddSingleton<IPubSubIdentityRegistry, PubSubIdentityRegistry>();
            services.TryAddSingleton<PubSubConfigurationTranslator>();
            services.TryAddSingleton<PubSubShadowEncodingRegistry>();
            services.TryAddSingleton<PubSubShadowRuntimeStateProvider>();
            services.TryAddSingleton<IPubSubShadowRuntimeStateProvider>(
                provider => provider.GetRequiredService<PubSubShadowRuntimeStateProvider>());
            services.TryAddSingleton<InMemoryPubSubShadowCaptureSink>(provider =>
            {
                var options = provider.GetRequiredService<IOptions<PubSubShadowCaptureOptions>>();
                return new InMemoryPubSubShadowCaptureSink(options.Value.Capacity);
            });
            services.TryAddSingleton<IPubSubShadowCaptureSink>(
                provider => provider.GetRequiredService<InMemoryPubSubShadowCaptureSink>());
            services.TryAddSingleton<IPubSubShadowCaptureStore>(
                provider => provider.GetRequiredService<InMemoryPubSubShadowCaptureSink>());
            services.TryAddSingleton<ManagedPubSubNotificationBuffer>(provider =>
            {
                var options = provider.GetRequiredService<
                    IOptions<ManagedPubSubNotificationBufferOptions>>();
                return new ManagedPubSubNotificationBuffer(options.Value.Capacity);
            });
            services.TryAddSingleton<IManagedPubSubNotificationBuffer>(
                provider => provider.GetRequiredService<ManagedPubSubNotificationBuffer>());
            services.TryAddSingleton<IManagedPubSubEventBuffer>(
                provider => provider.GetRequiredService<ManagedPubSubNotificationBuffer>());
            services.TryAddSingleton<PubSubShadowHost>();
            services.TryAddSingleton<IPubSubShadowHost>(
                provider => provider.GetRequiredService<PubSubShadowHost>());
            if (!registered)
            {
                services.AddSingleton<IHostedService>(
                    provider => provider.GetRequiredService<PubSubShadowHost>());
            }
            return services;
        }
    }

    internal sealed class PubSubShadowHost : IHostedService, IPubSubShadowHost,
        IAsyncDisposable
    {
        public PubSubShadowHost(IPubSubIdentityRegistry identityRegistry,
            PubSubConfigurationTranslator translator,
            PubSubShadowRuntimeStateProvider state,
            IServiceProvider services)
            : this(identityRegistry, translator, state,
                services.GetRequiredService<PubSubShadowEncodingRegistry>(),
                CreateApplication(services,
                    services.GetRequiredService<IPubSubShadowCaptureSink>(),
                    state, services.GetRequiredService<PubSubShadowEncodingRegistry>(),
                    out var configuration), configuration)
        {
        }

        internal PubSubShadowHost(IPubSubIdentityRegistry identityRegistry,
            PubSubConfigurationTranslator translator,
            PubSubShadowRuntimeStateProvider state,
            IPubSubApplication application,
            PubSubConfigurationDataType configuration)
            : this(identityRegistry, translator, state, new PubSubShadowEncodingRegistry(),
                application, configuration)
        {
        }

        internal PubSubShadowHost(IPubSubIdentityRegistry identityRegistry,
            PubSubConfigurationTranslator translator,
            PubSubShadowRuntimeStateProvider state,
            PubSubShadowEncodingRegistry encodingRegistry,
            IPubSubApplication application,
            PubSubConfigurationDataType configuration)
        {
            _identityRegistry = identityRegistry ??
                throw new ArgumentNullException(nameof(identityRegistry));
            _translator = translator ?? throw new ArgumentNullException(nameof(translator));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _encodingRegistry = encodingRegistry ??
                throw new ArgumentNullException(nameof(encodingRegistry));
            _application = application ?? throw new ArgumentNullException(nameof(application));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_started)
                {
                    return;
                }

                await _application.StartAsync(cancellationToken).ConfigureAwait(false);
                _started = true;
                _state.Started();
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (!_started)
                {
                    return;
                }

                await _application.StopAsync(cancellationToken).ConfigureAwait(false);
                _started = false;
                _state.Stopped();
            }
            finally
            {
                _gate.Release();
            }
        }

        public async ValueTask ReplaceConfigurationAsync(
            IEnumerable<WriterGroupModel> writerGroups,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(writerGroups);
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await using var transaction = await _identityRegistry.BeginAsync(cancellationToken)
                    .ConfigureAwait(false);
                var translation = _translator.TranslateWithEncodingRegistry(writerGroups, transaction);
                var replacement = translation.Configuration;
                var previousEncodings = _encodingRegistry.Snapshot;
                _encodingRegistry.Replace(translation.Encodings);
                var encodingsReplaced = true;
                var replaced = false;
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var statusCodes = await _application.ReplaceConfigurationAsync(replacement,
                        CancellationToken.None).ConfigureAwait(false);
                    replaced = true;
                    foreach (var statusCode in statusCodes)
                    {
                        if (!StatusCode.IsGood(statusCode))
                        {
                            throw new InvalidOperationException(
                                "The standard PubSub runtime rejected a configuration change.");
                        }
                    }

                    // The native runtime is now changed. Complete the durable
                    // identity commit even if the initiating caller has since
                    // canceled, so its state cannot diverge from the runtime.
                    await transaction.CommitAsync(CancellationToken.None).ConfigureAwait(false);
                    _configuration = replacement;
                    _state.Replaced(replacement.Connections.Count,
                        CountDataSetWriters(replacement));
                }
                catch (Exception exception)
                {
                    if (replaced)
                    {
                        try
                        {
                            var rollbackStatus = await _application.ReplaceConfigurationAsync(
                                _configuration, CancellationToken.None).ConfigureAwait(false);
                            EnsureSuccessfulReplacement(rollbackStatus);
                        }
                        catch (Exception rollbackException)
                        {
                            _state.Failed(rollbackException);
                            throw new PubSubShadowRollbackException(exception, rollbackException);
                        }
                    }
                    if (encodingsReplaced)
                    {
                        _encodingRegistry.Replace(previousEncodings);
                    }
                    _state.Failed(exception);
                    throw;
                }
            }
            finally
            {
                _gate.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }
            await StopAsync(CancellationToken.None).ConfigureAwait(false);
            await _application.DisposeAsync().ConfigureAwait(false);
            _gate.Dispose();
        }

        private static IPubSubApplication CreateApplication(IServiceProvider services,
            IPubSubShadowCaptureSink captureSink, PubSubShadowRuntimeStateProvider state,
            PubSubShadowEncodingRegistry encodingRegistry,
            out PubSubConfigurationDataType configuration)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(captureSink);
            ArgumentNullException.ThrowIfNull(state);
            ArgumentNullException.ThrowIfNull(encodingRegistry);
            configuration = PubSubConfigurationTranslator.CreateEmpty();
            return new PubSubApplicationBuilder(new ServiceProviderTelemetryContext(services))
                .WithApplicationId("azure-iiot-publisher-shadow")
                .UseConfiguration(configuration)
                .AddTransportFactory(new NoEgressPubSubTransportFactory(
                    Profiles.PubSubMqttJsonTransport, PubSubShadowEncoding.Json,
                    captureSink, state, encodingRegistry))
                .AddTransportFactory(new NoEgressPubSubTransportFactory(
                    Profiles.PubSubUdpUadpTransport, PubSubShadowEncoding.Uadp,
                    captureSink, state))
                .AddEncoder(new ShadowJsonEncoder(encodingRegistry))
                .AddEncoder(new Opc.Ua.PubSub.Encoding.Uadp.UadpEncoder())
                .AddDecoder(new Opc.Ua.PubSub.Encoding.Json.JsonDecoder())
                .AddDecoder(new Opc.Ua.PubSub.Encoding.Uadp.UadpDecoder())
                .Build();
        }

        private static int CountDataSetWriters(PubSubConfigurationDataType configuration)
        {
            var count = 0;
            foreach (var connection in configuration.Connections)
            {
                foreach (var writerGroup in connection.WriterGroups)
                {
                    count += writerGroup.DataSetWriters.Count;
                }
            }
            return count;
        }

        private static void EnsureSuccessfulReplacement(ArrayOf<StatusCode> statusCodes)
        {
            foreach (var statusCode in statusCodes)
            {
                if (!StatusCode.IsGood(statusCode))
                {
                    throw new InvalidOperationException(
                        "The standard PubSub runtime rejected a configuration change.");
                }
            }
        }

        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly IPubSubIdentityRegistry _identityRegistry;
        private readonly PubSubConfigurationTranslator _translator;
        private readonly PubSubShadowRuntimeStateProvider _state;
        private readonly PubSubShadowEncodingRegistry _encodingRegistry;
        private readonly IPubSubApplication _application;
        private PubSubConfigurationDataType _configuration;
        private bool _started;
        private int _disposed;
    }

    internal sealed class PubSubShadowRollbackException : Exception
    {
        public PubSubShadowRollbackException(Exception updateException,
            Exception rollbackException)
            : base("The shadow PubSub update and its native runtime rollback failed.",
                new AggregateException(updateException, rollbackException))
        {
        }
    }

    internal sealed class PubSubShadowConfigurationTranslation
    {
        public PubSubShadowConfigurationTranslation(PubSubConfigurationDataType configuration,
            PubSubShadowEncodingRegistrySnapshot encodings)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            Encodings = encodings ?? throw new ArgumentNullException(nameof(encodings));
        }

        public PubSubConfigurationDataType Configuration { get; }

        public PubSubShadowEncodingRegistrySnapshot Encodings { get; }
    }

    internal sealed class PubSubShadowEncodingRegistrySnapshot
    {
        public void Add(string connectionName, ushort writerGroupId,
            PubSubShadowEncoding encoding)
        {
            if (string.IsNullOrWhiteSpace(connectionName))
            {
                throw new ArgumentException("A connection name is required.", nameof(connectionName));
            }
            if (writerGroupId == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(writerGroupId));
            }
            if (!_connectionEncodings.TryAdd(connectionName, encoding)
                || !_writerGroupEncodings.TryAdd(writerGroupId, encoding))
            {
                throw new ArgumentException(
                    "A shadow PubSub encoding marker already exists for this connection or writer group.");
            }
        }

        internal bool TryGetConnectionEncoding(string connectionName,
            out PubSubShadowEncoding encoding)
        {
            return _connectionEncodings.TryGetValue(connectionName, out encoding);
        }

        internal bool TryGetWriterGroupEncoding(ushort writerGroupId,
            out PubSubShadowEncoding encoding)
        {
            return _writerGroupEncodings.TryGetValue(writerGroupId, out encoding);
        }

        internal PubSubShadowEncodingRegistrySnapshot Clone()
        {
            var copy = new PubSubShadowEncodingRegistrySnapshot();
            foreach (var entry in _connectionEncodings)
            {
                copy._connectionEncodings.Add(entry.Key, entry.Value);
            }
            foreach (var entry in _writerGroupEncodings)
            {
                copy._writerGroupEncodings.Add(entry.Key, entry.Value);
            }
            return copy;
        }

        private readonly Dictionary<string, PubSubShadowEncoding> _connectionEncodings =
            new(StringComparer.Ordinal);
        private readonly Dictionary<ushort, PubSubShadowEncoding> _writerGroupEncodings = [];
    }

    internal sealed class PubSubShadowEncodingRegistry
    {
        public PubSubShadowEncodingRegistrySnapshot Snapshot
        {
            get
            {
                lock (_gate)
                {
                    return _snapshot.Clone();
                }
            }
        }

        public void Replace(PubSubShadowEncodingRegistrySnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);
            lock (_gate)
            {
                _snapshot = snapshot.Clone();
            }
        }

        public PubSubShadowEncoding ResolveForWriterGroup(ushort? writerGroupId)
        {
            if (writerGroupId is not { } id)
            {
                throw new InvalidOperationException(
                    "The JSON NetworkMessage does not carry a writer group identity.");
            }
            lock (_gate)
            {
                if (_snapshot.TryGetWriterGroupEncoding(id, out var encoding))
                {
                    return encoding;
                }
            }
            throw new InvalidOperationException(
                $"No shadow PubSub encoding marker exists for writer group '{id}'.");
        }

        public PubSubShadowEncoding ResolveForConnection(PubSubConnectionDataType connection)
        {
            ArgumentNullException.ThrowIfNull(connection);
            var connectionName = connection.Name ?? string.Empty;
            lock (_gate)
            {
                if (!_snapshot.TryGetConnectionEncoding(connectionName, out var encoding))
                {
                    throw new InvalidOperationException(
                        $"No shadow PubSub encoding marker exists for connection '{connectionName}'.");
                }
                foreach (var writerGroup in connection.WriterGroups)
                {
                    if (!_snapshot.TryGetWriterGroupEncoding(writerGroup.WriterGroupId,
                        out var writerGroupEncoding)
                        || writerGroupEncoding != encoding)
                    {
                        throw new InvalidOperationException(
                            $"The shadow PubSub encoding markers for connection '{connectionName}' disagree.");
                    }
                }
                return encoding;
            }
        }

        private readonly Lock _gate = new();
        private PubSubShadowEncodingRegistrySnapshot _snapshot = new();
    }

    internal sealed class ShadowJsonEncoder : INetworkMessageEncoder
    {
        public ShadowJsonEncoder(PubSubShadowEncodingRegistry encodings)
        {
            _encodings = encodings ?? throw new ArgumentNullException(nameof(encodings));
        }

        public string TransportProfileUri => Profiles.PubSubMqttJsonTransport;

        public int EstimatedHeaderOverhead => _compact.EstimatedHeaderOverhead;

        public ValueTask<ReadOnlyMemory<byte>> EncodeAsync(PubSubNetworkMessage networkMessage,
            PubSubNetworkMessageContext context, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(networkMessage);
            var encoding = _encodings.ResolveForWriterGroup(networkMessage.WriterGroupId);
            return encoding switch
            {
                PubSubShadowEncoding.Json or PubSubShadowEncoding.JsonGzip =>
                    _compact.EncodeAsync(networkMessage, context, cancellationToken),
                PubSubShadowEncoding.JsonReversible or PubSubShadowEncoding.JsonReversibleGzip =>
                    _verbose.EncodeAsync(networkMessage, context, cancellationToken),
                _ => throw new InvalidOperationException(
                    $"Shadow JSON encoder cannot encode '{encoding}' messages.")
            };
        }

        private readonly PubSubShadowEncodingRegistry _encodings;
        private readonly Opc.Ua.PubSub.Encoding.Json.JsonEncoder _compact =
            new(JsonEncodingMode.Compact);
        private readonly Opc.Ua.PubSub.Encoding.Json.JsonEncoder _verbose =
            new(JsonEncodingMode.Verbose);
    }

    internal sealed class NoEgressPubSubTransportFactory : IPubSubTransportFactory
    {
        public NoEgressPubSubTransportFactory(string transportProfileUri,
            PubSubShadowEncoding encoding, IPubSubShadowCaptureSink captureSink,
            PubSubShadowRuntimeStateProvider state,
            PubSubShadowEncodingRegistry? encodingRegistry = null)
        {
            TransportProfileUri = transportProfileUri ??
                throw new ArgumentNullException(nameof(transportProfileUri));
            _encoding = encoding;
            _captureSink = captureSink ?? throw new ArgumentNullException(nameof(captureSink));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _encodingRegistry = encodingRegistry;
        }

        public string TransportProfileUri { get; }

        public IPubSubTransport Create(PubSubConnectionDataType connection,
            ITelemetryContext telemetry, TimeProvider timeProvider)
        {
            ArgumentNullException.ThrowIfNull(connection);
            ArgumentNullException.ThrowIfNull(telemetry);
            ArgumentNullException.ThrowIfNull(timeProvider);
            var direction = PubSubTransportDirection.None;
            if (!connection.WriterGroups.IsNull && connection.WriterGroups.Count > 0)
            {
                direction |= PubSubTransportDirection.Send;
            }
            if (!connection.ReaderGroups.IsNull && connection.ReaderGroups.Count > 0)
            {
                direction |= PubSubTransportDirection.Receive;
            }
            var encoding = _encodingRegistry?.ResolveForConnection(connection) ?? _encoding;
            return new NoEgressPubSubTransport(TransportProfileUri, encoding, direction,
                _captureSink, _state, timeProvider);
        }

        private readonly PubSubShadowEncoding _encoding;
        private readonly IPubSubShadowCaptureSink _captureSink;
        private readonly PubSubShadowRuntimeStateProvider _state;
        private readonly PubSubShadowEncodingRegistry? _encodingRegistry;
    }

    internal sealed class NoEgressPubSubTransport : IPubSubTransport
    {
        public NoEgressPubSubTransport(string transportProfileUri,
            PubSubShadowEncoding encoding, PubSubTransportDirection direction,
            IPubSubShadowCaptureSink captureSink, PubSubShadowRuntimeStateProvider state,
            TimeProvider timeProvider)
        {
            TransportProfileUri = transportProfileUri;
            _encoding = encoding;
            Direction = direction;
            _captureSink = captureSink;
            _state = state;
            _timeProvider = timeProvider;
        }

        public string TransportProfileUri { get; }

        public PubSubTransportDirection Direction { get; }

        public bool IsConnected => _isConnected;

        public event EventHandler<PubSubTransportStateChangedEventArgs>? StateChanged;

        public ValueTask OpenAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _isConnected = true;
            StateChanged?.Invoke(this, new PubSubTransportStateChangedEventArgs(
                true, StatusCodes.Good, "No-egress shadow capture transport opened."));
            return default;
        }

        public ValueTask CloseAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _isConnected = false;
            StateChanged?.Invoke(this, new PubSubTransportStateChangedEventArgs(
                false, StatusCodes.Good, "No-egress shadow capture transport closed."));
            return default;
        }

        public async ValueTask SendAsync(ReadOnlyMemory<byte> payload, string? topic = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _captureSink.CaptureAsync(new PubSubShadowCapture(_encoding,
                _timeProvider.GetUtcNow(), TransportProfileUri,
                CompressIfRequired(payload, _encoding).Span),
                cancellationToken).ConfigureAwait(false);
            _state.Captured();
        }

        public async IAsyncEnumerable<PubSubTransportFrame> ReceiveAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask.ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            yield break;
        }

        public async ValueTask DisposeAsync()
        {
            await CloseAsync().ConfigureAwait(false);
        }

        private static ReadOnlyMemory<byte> CompressIfRequired(ReadOnlyMemory<byte> payload,
            PubSubShadowEncoding encoding)
        {
            if (encoding is not (PubSubShadowEncoding.JsonGzip
                or PubSubShadowEncoding.JsonReversibleGzip))
            {
                return payload;
            }
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionLevel.SmallestSize, true))
            {
                gzip.Write(payload.Span);
            }
            return output.ToArray();
        }

        private readonly PubSubShadowEncoding _encoding;
        private readonly IPubSubShadowCaptureSink _captureSink;
        private readonly PubSubShadowRuntimeStateProvider _state;
        private readonly TimeProvider _timeProvider;
        private bool _isConnected;
    }
}
