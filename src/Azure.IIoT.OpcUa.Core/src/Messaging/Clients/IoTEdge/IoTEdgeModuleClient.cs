// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.IoTEdge
{
    using Azure.IIoT.OpcUa.Core.IoTEdge;
    using global::IoTHubby;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Shared IoTHubby module client holder.
    /// </summary>
    public sealed class IoTEdgeModuleClient : IAsyncDisposable
    {
        /// <summary>
        /// Module client.
        /// </summary>
        public IoTHubModuleClient Client => _client is IoTHubModuleClientFactory.Adapter adapter ?
            adapter.Client :
            throw new InvalidOperationException(
                "The concrete IoTHubby module client is not available.");

        /// <summary>
        /// Identity.
        /// </summary>
        public IIoTEdgeDeviceIdentity Identity { get; }

        /// <summary>
        /// Create client.
        /// </summary>
        public IoTEdgeModuleClient(IOptions<IoTEdgeClientOptions> options,
            IIoTEdgeDeviceIdentity identity,
            IEnumerable<IIoTEdgeClientState> stateHandlers,
            ILoggerFactory? loggerFactory = null,
            IIoTHubModuleClientFactory? clientFactory = null)
        {
            ArgumentNullException.ThrowIfNull(options);
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
            _stateHandlers = stateHandlers ?? [];
            void Configure(IoTHubClientOptions clientOptions)
            {
                clientOptions.ProductInfo = options.Value.Product;
                clientOptions.LoggerFactory = loggerFactory;
                if (options.Value.KeepAlivePeriodSeconds is > 0)
                {
                    clientOptions.KeepAlive =
                        TimeSpan.FromSeconds(options.Value.KeepAlivePeriodSeconds.Value);
                }
                if (options.Value.DefaultMethodCallTimeout != null)
                {
                    clientOptions.OperationTimeout =
                        options.Value.DefaultMethodCallTimeout.Value;
                }
            }

            _client = (clientFactory ?? IoTHubModuleClientFactory.Instance)
                .Create(options.Value, Configure);
            _client.ConnectionStateChanged += OnConnectionStateChanged;
        }

        /// <summary>
        /// Ensure connected.
        /// </summary>
        public async Task EnsureConnectedAsync(CancellationToken ct)
        {
            if (_client.State == IoTHubConnectionState.Connected)
            {
                return;
            }
            await _connectLock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (_client.State != IoTHubConnectionState.Connected)
                {
                    await _client.ConnectAsync(ct).ConfigureAwait(false);
                }
            }
            finally
            {
                _connectLock.Release();
            }
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            _client.ConnectionStateChanged -= OnConnectionStateChanged;
            await _client.DisposeAsync().ConfigureAwait(false);
            foreach (var handler in _stateHandlers)
            {
                handler.OnClosed(_counter, Identity.DeviceId, Identity.ModuleId,
                    "Disposed");
            }
            _connectLock.Dispose();
        }

        private void OnConnectionStateChanged(object? sender,
            IoTHubConnectionStateChangedEventArgs args)
        {
            var reason = args.Reason ?? args.State.ToString();
            var counter = Interlocked.Increment(ref _counter);
            foreach (var handler in _stateHandlers)
            {
                switch (args.State)
                {
                    case IoTHubConnectionState.Connected:
                        if (Interlocked.Exchange(ref _opened, 1) == 0)
                        {
                            handler.OnOpened(counter, Identity.DeviceId, Identity.ModuleId);
                        }
                        else
                        {
                            handler.OnConnected(counter, Identity.DeviceId,
                                Identity.ModuleId, reason);
                        }
                        break;
                    case IoTHubConnectionState.Disconnected:
                        handler.OnDisconnected(counter, Identity.DeviceId,
                            Identity.ModuleId, reason);
                        break;
                    case IoTHubConnectionState.Reconnecting:
                        handler.OnError(counter, Identity.DeviceId,
                            Identity.ModuleId, reason);
                        break;
                    case IoTHubConnectionState.Disposed:
                        handler.OnClosed(counter, Identity.DeviceId,
                            Identity.ModuleId, reason);
                        break;
                }
            }
        }

        internal Task SetMethodHandlerAsync(Func<DirectMethodRequest, CancellationToken,
            ValueTask<DirectMethodResponse>>? handler, CancellationToken ct = default)
        {
            return _client.SetMethodHandlerAsync(handler, ct);
        }

        internal ValueTask SendTelemetryAsync(TelemetryMessage message,
            CancellationToken ct)
        {
            return _client.SendTelemetryAsync(message, ct);
        }

        internal ValueTask SendToOutputAsync(string outputName,
            TelemetryMessage message, CancellationToken ct)
        {
            return _client.SendToOutputAsync(outputName, message, ct);
        }

        internal IAsyncEnumerable<CloudToDeviceMessage> ReceiveInputMessagesAsync(
            string inputName, CancellationToken ct)
        {
            return _client.ReceiveInputMessagesAsync(inputName, ct);
        }

        internal Task<Twin> GetTwinAsync(CancellationToken ct)
        {
            return _client.GetTwinAsync(ct);
        }

        internal Task<long?> UpdateReportedPropertiesAsync(string json,
            CancellationToken ct)
        {
            return _client.UpdateReportedPropertiesAsync(json, ct);
        }

        private readonly IIoTHubModuleClient _client;
        private readonly IEnumerable<IIoTEdgeClientState> _stateHandlers;
        private readonly SemaphoreSlim _connectLock = new(1, 1);
        private int _counter;
        private int _opened;
    }
}
