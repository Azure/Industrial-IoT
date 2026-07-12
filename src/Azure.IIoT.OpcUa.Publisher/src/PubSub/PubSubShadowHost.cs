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
    using Opc.Ua;
    using Opc.Ua.PubSub.Application;
    using Opc.Ua.PubSub.Transports;
    using System;
    using System.Collections.Generic;
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

            services.AddOptions<PublisherOptions>();
            services.TryAddSingleton<IPubSubIdentityRegistryStore,
                FilePubSubIdentityRegistryStore>();
            services.TryAddSingleton<IPubSubIdentityRegistry, PubSubIdentityRegistry>();
            services.TryAddSingleton<PubSubConfigurationTranslator>();
            services.TryAddSingleton<PubSubShadowRuntimeStateProvider>();
            services.TryAddSingleton<IPubSubShadowRuntimeStateProvider>(
                provider => provider.GetRequiredService<PubSubShadowRuntimeStateProvider>());
            services.TryAddSingleton<InMemoryPubSubShadowCaptureSink>();
            services.TryAddSingleton<IPubSubShadowCaptureSink>(
                provider => provider.GetRequiredService<InMemoryPubSubShadowCaptureSink>());
            services.TryAddSingleton<IPubSubShadowCaptureStore>(
                provider => provider.GetRequiredService<InMemoryPubSubShadowCaptureSink>());
            services.TryAddSingleton<ManagedPubSubNotificationBuffer>();
            services.TryAddSingleton<IManagedPubSubNotificationBuffer>(
                provider => provider.GetRequiredService<ManagedPubSubNotificationBuffer>());
            services.TryAddSingleton<IManagedPubSubEventBuffer>(
                provider => provider.GetRequiredService<ManagedPubSubNotificationBuffer>());
            services.TryAddSingleton<PubSubShadowEncodingBridge>();
            services.TryAddSingleton<PubSubShadowHost>();
            services.TryAddSingleton<IPubSubShadowHost>(
                provider => provider.GetRequiredService<PubSubShadowHost>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService>(
                provider => provider.GetRequiredService<PubSubShadowHost>()));
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
                CreateApplication(services,
                    services.GetRequiredService<IPubSubShadowCaptureSink>(),
                    state, out var configuration), configuration)
        {
        }

        internal PubSubShadowHost(IPubSubIdentityRegistry identityRegistry,
            PubSubConfigurationTranslator translator,
            PubSubShadowRuntimeStateProvider state,
            IPubSubApplication application,
            PubSubConfigurationDataType configuration)
        {
            _identityRegistry = identityRegistry ??
                throw new ArgumentNullException(nameof(identityRegistry));
            _translator = translator ?? throw new ArgumentNullException(nameof(translator));
            _state = state ?? throw new ArgumentNullException(nameof(state));
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
                var replacement = _translator.Translate(writerGroups, transaction);
                var replaced = false;
                try
                {
                    var statusCodes = await _application.ReplaceConfigurationAsync(replacement,
                        cancellationToken).ConfigureAwait(false);
                    replaced = true;
                    foreach (var statusCode in statusCodes)
                    {
                        if (!StatusCode.IsGood(statusCode))
                        {
                            throw new InvalidOperationException(
                                "The standard PubSub runtime rejected a configuration change.");
                        }
                    }

                    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                    _configuration = replacement;
                    _state.Replaced(replacement.Connections.Count,
                        CountDataSetWriters(replacement));
                }
                catch (Exception exception)
                {
                    if (replaced)
                    {
                        await _application.ReplaceConfigurationAsync(_configuration,
                            cancellationToken).ConfigureAwait(false);
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
            await StopAsync(CancellationToken.None).ConfigureAwait(false);
            await _application.DisposeAsync().ConfigureAwait(false);
            _gate.Dispose();
        }

        private static IPubSubApplication CreateApplication(IServiceProvider services,
            IPubSubShadowCaptureSink captureSink, PubSubShadowRuntimeStateProvider state,
            out PubSubConfigurationDataType configuration)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(captureSink);
            ArgumentNullException.ThrowIfNull(state);
            configuration = PubSubConfigurationTranslator.CreateEmpty();
            return new PubSubApplicationBuilder(new ServiceProviderTelemetryContext(services))
                .WithApplicationId("azure-iiot-publisher-shadow")
                .UseConfiguration(configuration)
                .AddTransportFactory(new NoEgressPubSubTransportFactory(
                    Profiles.PubSubMqttJsonTransport, PubSubShadowEncoding.Json,
                    captureSink, state))
                .AddTransportFactory(new NoEgressPubSubTransportFactory(
                    Profiles.PubSubUdpUadpTransport, PubSubShadowEncoding.Uadp,
                    captureSink, state))
                .UseAllStandardEncoders()
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

        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly IPubSubIdentityRegistry _identityRegistry;
        private readonly PubSubConfigurationTranslator _translator;
        private readonly PubSubShadowRuntimeStateProvider _state;
        private readonly IPubSubApplication _application;
        private PubSubConfigurationDataType _configuration;
        private bool _started;
    }

    internal sealed class NoEgressPubSubTransportFactory : IPubSubTransportFactory
    {
        public NoEgressPubSubTransportFactory(string transportProfileUri,
            PubSubShadowEncoding encoding, IPubSubShadowCaptureSink captureSink,
            PubSubShadowRuntimeStateProvider state)
        {
            TransportProfileUri = transportProfileUri ??
                throw new ArgumentNullException(nameof(transportProfileUri));
            _encoding = encoding;
            _captureSink = captureSink ?? throw new ArgumentNullException(nameof(captureSink));
            _state = state ?? throw new ArgumentNullException(nameof(state));
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
            return new NoEgressPubSubTransport(TransportProfileUri, _encoding, direction,
                _captureSink, _state, timeProvider);
        }

        private readonly PubSubShadowEncoding _encoding;
        private readonly IPubSubShadowCaptureSink _captureSink;
        private readonly PubSubShadowRuntimeStateProvider _state;
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
                _timeProvider.GetUtcNow(), payload.Span), cancellationToken).ConfigureAwait(false);
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

        private readonly PubSubShadowEncoding _encoding;
        private readonly IPubSubShadowCaptureSink _captureSink;
        private readonly PubSubShadowRuntimeStateProvider _state;
        private readonly TimeProvider _timeProvider;
        private bool _isConnected;
    }
}
