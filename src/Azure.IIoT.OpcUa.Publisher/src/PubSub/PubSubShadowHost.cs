// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.PubSub
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Core.Messaging;
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
    /// Test-only compatibility control for an all-writer native keyframe.
    /// It is intentionally separate from the public shadow-host contract.
    /// </summary>
    internal interface IPubSubKeyFrameControl
    {
        ValueTask ForceKeyFrameAsync(string writerGroupId,
            string? dataSetWriterId = null,
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
            services.TryAddEnumerable(ServiceDescriptor.Singleton<
                IManagedPubSubDataSourceProvider,
                ManagedPubSubNotificationDataSourceProvider>());
            services.TryAddSingleton<ManagedPubSubDataSetSourceRegistry>();
            services.TryAddSingleton<PubSubShadowHost>();
            services.TryAddSingleton<IPubSubShadowHost>(
                provider => provider.GetRequiredService<PubSubShadowHost>());
            services.TryAddSingleton<IPubSubKeyFrameControl>(
                provider => provider.GetRequiredService<PubSubShadowHost>());
            if (!registered)
            {
                services.AddSingleton<IHostedService>(
                    provider => provider.GetRequiredService<PubSubShadowHost>());
            }
            return services;
        }

        /// <summary>
        /// Registers the event-client transport only for an explicit test
        /// composition. No Publisher configuration path invokes this method.
        /// </summary>
        internal static IServiceCollection AddPubSubShadowEgressHost(
            this IServiceCollection services, IEventClient eventClient,
            Action<PubSubShadowEgressOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(eventClient);
            if (services.Any(descriptor =>
                descriptor.ServiceType == typeof(PubSubShadowEgressRegistration)))
            {
                throw new InvalidOperationException(
                    "The test-only PubSub shadow egress transport is already registered.");
            }
            var options = new PubSubShadowEgressOptions();
            configure?.Invoke(options);
            services.AddPubSubShadowHost();
            services.AddSingleton(new PubSubShadowEgressRegistration(eventClient, options));
            return services;
        }
    }

    internal sealed class PubSubShadowHost : IHostedService, IPubSubShadowHost,
        IPubSubKeyFrameControl,
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
            _publisherOptions = services.GetRequiredService<IOptions<PublisherOptions>>();
            _managedDataSources = services.GetRequiredService<
                ManagedPubSubDataSetSourceRegistry>();
            _egress = services.GetService<PubSubShadowEgressRegistration>();
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
                var groups = writerGroups.ToArray();
                await using var transaction = await _identityRegistry.BeginAsync(cancellationToken)
                    .ConfigureAwait(false);
                var translation = _translator.TranslateWithEncodingRegistry(groups, transaction);
                var replacement = translation.Configuration;
                var previousGeneration = _encodingRegistry.ActiveGeneration;
                var previousEgress = _egress?.Settings.Snapshot();
                await using var sourceTransaction = _managedDataSources is null
                    ? null
                    : await _managedDataSources.PrepareAsync(groups, cancellationToken)
                        .ConfigureAwait(false);
                var wasStarted = _started;
                var stopped = false;
                var replaced = false;
                var encodingsReplaced = false;
                var egressReplaced = false;
                var committed = false;
                var tombstones = Array.Empty<RetainedMetaDataTombstone>();
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    sourceTransaction?.Install();
                    if (_egress is not null)
                    {
                        _egress.Settings.Replace(groups, _publisherOptions?.Value
                            ?? new PublisherOptions(), _egress.Options);
                        egressReplaced = true;
                        tombstones = GetRemovedMetaDataTopics(_configuration, previousEgress,
                            replacement, _egress.Settings.Snapshot());
                        foreach (var tombstone in tombstones)
                        {
                            EventClientPubSubTransportFactory.ValidateTombstoneCapability(
                                _egress.EventClient);
                            EventClientPubSubTransportFactory.ValidateCapabilities(
                                _egress.EventClient, tombstone.Settings.RequiredCapabilities);
                        }
                    }
                    if (wasStarted)
                    {
                        await _application.StopAsync(CancellationToken.None)
                            .ConfigureAwait(false);
                        stopped = true;
                    }
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

                    _encodingRegistry.Replace(translation.Encodings);
                    encodingsReplaced = true;
                    await transaction.CommitAsync(CancellationToken.None).ConfigureAwait(false);
                    if (wasStarted)
                    {
                        await _application.StartAsync(CancellationToken.None)
                            .ConfigureAwait(false);
                    }
                    if (sourceTransaction is not null)
                    {
                        await sourceTransaction.CommitAsync().ConfigureAwait(false);
                    }
                    _configuration = replacement;
                    committed = true;
                    _state.Replaced(replacement.Connections.Count,
                        CountDataSetWriters(replacement));
                    foreach (var tombstone in tombstones)
                    {
                        try
                        {
                            await EventClientPubSubTransportFactory.SendMetadataTombstoneAsync(
                                _egress!.EventClient, tombstone.Settings, tombstone.Topic,
                                TimeProvider.System, cancellationToken).ConfigureAwait(false);
                        }
                        catch (Exception exception)
                        {
                            // The replacement is already durable and running. Do not
                            // report it as rolled back; retain the cleanup failure in
                            // shadow diagnostics for a retrying cutover controller.
                            _state.Failed(exception);
                        }
                    }
                }
                catch (Exception exception)
                {
                    if (committed)
                    {
                        _state.Failed(exception);
                        throw;
                    }
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
                        finally
                        {
                            if (encodingsReplaced)
                            {
                                _encodingRegistry.Restore(previousGeneration);
                                encodingsReplaced = false;
                            }
                            if (egressReplaced && previousEgress is not null)
                            {
                                _egress!.Settings.Restore(previousEgress);
                                egressReplaced = false;
                            }
                        }
                    }
                    if (encodingsReplaced)
                    {
                        _encodingRegistry.Restore(previousGeneration);
                    }
                    if (egressReplaced && previousEgress is not null)
                    {
                        _egress!.Settings.Restore(previousEgress);
                    }
                    if (stopped && wasStarted)
                    {
                        await _application.StartAsync(CancellationToken.None)
                            .ConfigureAwait(false);
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

        public async ValueTask ForceKeyFrameAsync(string writerGroupId,
            string? dataSetWriterId = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(writerGroupId))
            {
                throw new ArgumentException("A writer group identifier is required.",
                    nameof(writerGroupId));
            }
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (!_started)
                {
                    throw new InvalidOperationException(
                        "The shadow host must be running to force a native keyframe.");
                }
                var connectionName = "shadow-" + writerGroupId;
                var connection = _application.Connections.SingleOrDefault(candidate =>
                    string.Equals(candidate.Name, connectionName, StringComparison.Ordinal));
                if (connection is null || connection.WriterGroups.Count != 1
                    || connection.WriterGroups[0] is not Opc.Ua.PubSub.Groups.WriterGroup group)
                {
                    throw new InvalidOperationException(
                        $"The native writer group '{writerGroupId}' is not available.");
                }
                if (dataSetWriterId is not null)
                {
                    var nativeId = await _identityRegistry.TryGetIdAsync(
                        "data-set-writer", dataSetWriterId, cancellationToken).ConfigureAwait(false);
                    var found = false;
                    for (var index = 0; nativeId is not null
                        && index < group.DataSetWriters.Count; index++)
                    {
                        if (group.DataSetWriters[index].DataSetWriterId == nativeId.Value)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        throw new ArgumentException(
                            $"Data set writer '{dataSetWriterId}' is not in writer group '{writerGroupId}'.",
                            nameof(dataSetWriterId));
                    }
                }

                // The stack has no public force-keyframe operation. Replacing the
                // same native configuration resets its per-writer snapshot state;
                // the first explicit PublishOnceAsync is therefore a complete,
                // native keyframe for every writer in the group.
                _managedDataSources?.RequestKeyFrame();
                await _application.StopAsync(CancellationToken.None).ConfigureAwait(false);
                var statusCodes = await _application.ReplaceConfigurationAsync(_configuration,
                    CancellationToken.None).ConfigureAwait(false);
                EnsureSuccessfulReplacement(statusCodes);
                await _application.StartAsync(CancellationToken.None).ConfigureAwait(false);

                connection = _application.Connections.Single(candidate =>
                    string.Equals(candidate.Name, connectionName, StringComparison.Ordinal));
                group = connection.WriterGroups[0] as Opc.Ua.PubSub.Groups.WriterGroup
                    ?? throw new InvalidOperationException(
                        "The native PubSub writer group does not expose PublishOnceAsync.");
                await group.EnableAsync(cancellationToken).ConfigureAwait(false);
                await group.PublishOnceAsync(cancellationToken).ConfigureAwait(false);
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
            var egress = services.GetService<PubSubShadowEgressRegistration>();
            var builder = new PubSubApplicationBuilder(new ServiceProviderTelemetryContext(services))
                .WithApplicationId("azure-iiot-publisher-shadow")
                .UseConfiguration(configuration)
                .AddEncoder(new ShadowJsonEncoder(encodingRegistry))
                .AddEncoder(new Opc.Ua.PubSub.Encoding.Uadp.UadpEncoder())
                .AddDecoder(new Opc.Ua.PubSub.Encoding.Json.JsonDecoder())
                .AddDecoder(new Opc.Ua.PubSub.Encoding.Uadp.UadpDecoder())
                .WithDataSetSourceProvider(services.GetRequiredService<
                    ManagedPubSubDataSetSourceRegistry>());
            if (egress is null)
            {
                builder
                    .AddTransportFactory(new NoEgressPubSubTransportFactory(
                        Profiles.PubSubMqttJsonTransport, PubSubShadowEncoding.Json,
                        captureSink, state, encodingRegistry))
                    .AddTransportFactory(new NoEgressPubSubTransportFactory(
                        Profiles.PubSubUdpUadpTransport, PubSubShadowEncoding.Uadp,
                        captureSink, state));
            }
            else
            {
                builder
                    .AddTransportFactory(new EventClientPubSubTransportFactory(
                        Profiles.PubSubMqttJsonTransport, egress.EventClient,
                        egress.Settings, egress.Options))
                    .AddTransportFactory(new EventClientPubSubTransportFactory(
                        Profiles.PubSubUdpUadpTransport, egress.EventClient,
                        egress.Settings, egress.Options));
            }
            return builder.Build();
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

        private static RetainedMetaDataTombstone[] GetRemovedMetaDataTopics(
            PubSubConfigurationDataType previous,
            Dictionary<string, PubSubShadowEgressSettings>? previousSettings,
            PubSubConfigurationDataType replacement,
            Dictionary<string, PubSubShadowEgressSettings> replacementSettings)
        {
            if (previousSettings is null)
            {
                return [];
            }
            var activeTopics = new Dictionary<string, List<PubSubShadowEgressSettings>>(
                StringComparer.Ordinal);
            foreach (var connection in replacement.Connections)
            {
                if (!replacementSettings.TryGetValue(connection.Name ?? string.Empty,
                    out var settings))
                {
                    continue;
                }
                foreach (var topic in EventClientPubSubTransportFactory
                    .CreateMetadataRouting(connection, settings).ByTopic)
                {
                    if (!activeTopics.TryGetValue(topic.Key, out var active))
                    {
                        active = [];
                        activeTopics.Add(topic.Key, active);
                    }
                    active.Add(topic.Value);
                }
            }

            var removed = new List<RetainedMetaDataTombstone>();
            foreach (var connection in previous.Connections)
            {
                if (!previousSettings.TryGetValue(connection.Name ?? string.Empty,
                    out var settings))
                {
                    continue;
                }
                foreach (var entry in EventClientPubSubTransportFactory
                    .CreateMetadataRouting(connection, settings).ByTopic)
                {
                    if (entry.Value.Retain
                        && (!activeTopics.TryGetValue(entry.Key, out var active)
                            || !active.Any(settings => settings.Retain)))
                    {
                        removed.Add(new RetainedMetaDataTombstone(entry.Key, entry.Value));
                    }
                }
            }
            return [.. removed];
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
        private IOptions<PublisherOptions>? _publisherOptions;
        private ManagedPubSubDataSetSourceRegistry? _managedDataSources;
        private PubSubShadowEgressRegistration? _egress;
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

    internal sealed record class RetainedMetaDataTombstone(string Topic,
        PubSubShadowEgressSettings Settings);

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

    internal sealed class PubSubShadowEncodingMarker
    {
        public PubSubShadowEncodingMarker(PubSubShadowEncodingGeneration? generation,
            PubSubShadowEncoding encoding)
        {
            Generation = generation;
            Encoding = encoding;
        }

        public PubSubShadowEncodingGeneration? Generation { get; }

        public PubSubShadowEncoding Encoding { get; }
    }

    internal sealed class PubSubShadowEncodingGeneration
    {
        public PubSubShadowEncodingGeneration(long id,
            PubSubShadowEncodingRegistrySnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);
            Id = id;
            _snapshot = snapshot.Clone();
        }

        public long Id { get; }

        public PubSubShadowEncodingMarker ResolveForWriterGroup(ushort? writerGroupId)
        {
            if (writerGroupId is not { } id)
            {
                throw new InvalidOperationException(
                    "The JSON NetworkMessage does not carry a writer group identity.");
            }
            if (_snapshot.TryGetWriterGroupEncoding(id, out var encoding))
            {
                return new PubSubShadowEncodingMarker(this, encoding);
            }
            throw new InvalidOperationException(
                $"No shadow PubSub encoding marker exists for writer group '{id}'.");
        }

        public PubSubShadowEncodingMarker ResolveForConnection(
            PubSubConnectionDataType connection)
        {
            ArgumentNullException.ThrowIfNull(connection);
            var connectionName = connection.Name ?? string.Empty;
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
            return new PubSubShadowEncodingMarker(this, encoding);
        }

        private readonly PubSubShadowEncodingRegistrySnapshot _snapshot;
    }

    internal sealed class PubSubShadowEncodingRegistry
    {
        public PubSubShadowEncodingGeneration ActiveGeneration
        {
            get
            {
                lock (_gate)
                {
                    return _activeGeneration;
                }
            }
        }

        public void Replace(PubSubShadowEncodingRegistrySnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);
            lock (_gate)
            {
                _activeGeneration = new PubSubShadowEncodingGeneration(
                    checked(++_nextGeneration), snapshot);
            }
        }

        public void Restore(PubSubShadowEncodingGeneration generation)
        {
            ArgumentNullException.ThrowIfNull(generation);
            lock (_gate)
            {
                _activeGeneration = generation;
            }
        }

        private readonly Lock _gate = new();
        private long _nextGeneration;
        private PubSubShadowEncodingGeneration _activeGeneration = new(0, new());
    }

    internal interface IPubSubShadowEncodingObserver
    {
        ValueTask BeforeEncodeAsync(PubSubShadowEncodingMarker marker,
            PubSubNetworkMessage networkMessage,
            CancellationToken cancellationToken = default);
    }

    internal sealed class ShadowJsonEncoder : INetworkMessageEncoder
    {
        public ShadowJsonEncoder(PubSubShadowEncodingRegistry encodings,
            IPubSubShadowEncodingObserver? observer = null)
        {
            _encodings = encodings ?? throw new ArgumentNullException(nameof(encodings));
            _observer = observer;
        }

        public string TransportProfileUri => Profiles.PubSubMqttJsonTransport;

        public int EstimatedHeaderOverhead => _compact.EstimatedHeaderOverhead;

        public async ValueTask<ReadOnlyMemory<byte>> EncodeAsync(PubSubNetworkMessage networkMessage,
            PubSubNetworkMessageContext context, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(networkMessage);
            var marker = _encodings.ActiveGeneration.ResolveForWriterGroup(
                networkMessage.WriterGroupId);
            if (_observer is not null)
            {
                await _observer.BeforeEncodeAsync(marker, networkMessage, cancellationToken)
                    .ConfigureAwait(false);
            }
            return marker.Encoding switch
            {
                PubSubShadowEncoding.Json or PubSubShadowEncoding.JsonGzip =>
                    await _compact.EncodeAsync(networkMessage, context, cancellationToken)
                        .ConfigureAwait(false),
                PubSubShadowEncoding.JsonReversible or PubSubShadowEncoding.JsonReversibleGzip =>
                    await _verbose.EncodeAsync(networkMessage, context, cancellationToken)
                        .ConfigureAwait(false),
                _ => throw new InvalidOperationException(
                    $"Shadow JSON encoder cannot encode '{marker.Encoding}' messages.")
            };
        }

        private readonly PubSubShadowEncodingRegistry _encodings;
        private readonly IPubSubShadowEncodingObserver? _observer;
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
            var marker = _encodingRegistry?.ActiveGeneration.ResolveForConnection(connection)
                ?? new PubSubShadowEncodingMarker(null, _encoding);
            return new NoEgressPubSubTransport(TransportProfileUri, marker, direction,
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
            PubSubShadowEncodingMarker marker, PubSubTransportDirection direction,
            IPubSubShadowCaptureSink captureSink, PubSubShadowRuntimeStateProvider state,
            TimeProvider timeProvider)
        {
            TransportProfileUri = transportProfileUri;
            _marker = marker ?? throw new ArgumentNullException(nameof(marker));
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
            await _captureSink.CaptureAsync(new PubSubShadowCapture(_marker.Encoding,
                _timeProvider.GetUtcNow(), TransportProfileUri,
                CompressIfRequired(payload, _marker.Encoding).Span),
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

        private readonly PubSubShadowEncodingMarker _marker;
        private readonly IPubSubShadowCaptureSink _captureSink;
        private readonly PubSubShadowRuntimeStateProvider _state;
        private readonly TimeProvider _timeProvider;
        private bool _isConnected;
    }
}
