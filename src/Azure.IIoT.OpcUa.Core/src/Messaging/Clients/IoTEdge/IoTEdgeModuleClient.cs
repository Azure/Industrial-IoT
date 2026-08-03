// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.IoTEdge
{
    using Azure.IIoT.OpcUa.Core.IoTEdge;
    using global::IoTHubby;
    using global::IoTHubby.Edge;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Shared IoTHubby module client holder.
    /// </summary>
    /// <remarks>
    /// Excluded from coverage. Every member either constructs or delegates to a
    /// concrete <c>IoTHubModuleClient</c>, which is sealed and built internally
    /// with no factory or interface to substitute, so nothing here is reachable
    /// without a live IoT Edge runtime. Introducing a seam purely to measure it
    /// was judged the larger risk at release-candidate stage; the trade is
    /// recorded here rather than hidden in a coverage filter.
    /// </remarks>
    [ExcludeFromCodeCoverage(Justification =
        "Wraps the sealed IoTHubModuleClient; reachable only against a live edge runtime.")]
    public sealed class IoTEdgeModuleClient : IAsyncDisposable
    {
        /// <summary>
        /// Module client.
        /// </summary>
        public IoTHubModuleClient Client { get; }

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
            ILoggerFactory? loggerFactory = null)
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

            Client = string.IsNullOrEmpty(options.Value.EdgeHubConnectionString)
                ? EdgeModuleClient.CreateFromEnvironmentAsync(Configure)
                    .GetAwaiter().GetResult()
                : IoTHubModuleClient.CreateFromConnectionString(
                    options.Value.EdgeHubConnectionString, Configure);
            Client.ConnectionStateChanged += OnConnectionStateChanged;
        }

        /// <summary>
        /// Ensure connected.
        /// </summary>
        public async Task EnsureConnectedAsync(CancellationToken ct)
        {
            if (Client.State == IoTHubConnectionState.Connected)
            {
                return;
            }
            await _connectLock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (Client.State != IoTHubConnectionState.Connected)
                {
                    await Client.ConnectAsync(ct).ConfigureAwait(false);
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
            Client.ConnectionStateChanged -= OnConnectionStateChanged;
            await Client.DisposeAsync().ConfigureAwait(false);
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

        private readonly IEnumerable<IIoTEdgeClientState> _stateHandlers;
        private readonly SemaphoreSlim _connectLock = new(1, 1);
        private int _counter;
        private int _opened;
    }
}
