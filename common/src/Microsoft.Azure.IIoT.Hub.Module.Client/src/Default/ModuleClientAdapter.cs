// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Shared;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Diagnostics.Metrics;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Adapts module client to interface
    /// </summary>
    public sealed class ModuleClientAdapter : IClient {
        /// <summary>
        /// Whether the client is closed
        /// </summary>
        public bool IsClosed { get; internal set; }

        /// <inheritdoc />
        public int MaxEventBufferSize => 256 * 1024;

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="client"></param>
        /// <param name="enableOutputRouting"></param>
        /// <param name="metrics"></param>
        internal ModuleClientAdapter(ModuleClient client, bool enableOutputRouting,
            IMetricsContext metrics)
            : this(metrics ?? throw new ArgumentNullException(nameof(metrics))) {
            _client = client ??
                throw new ArgumentNullException(nameof(client));
            _enableOutputRouting = enableOutputRouting;
        }

        /// <summary>
        /// Factory
        /// </summary>
        /// <param name="product"></param>
        /// <param name="cs"></param>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="enableOutputRouting"></param>
        /// <param name="transportSetting"></param>
        /// <param name="timeout"></param>
        /// <param name="retry"></param>
        /// <param name="onConnectionLost"></param>
        /// <param name="logger"></param>
        /// <param name="metrics"></param>
        /// <returns></returns>
        public static async Task<IClient> CreateAsync(string product,
            IotHubConnectionStringBuilder cs, string deviceId, string moduleId,
            bool enableOutputRouting, ITransportSettings transportSetting,
            TimeSpan timeout, IRetryPolicy retry, Action onConnectionLost, ILogger logger,
            IMetricsContext metrics) {
            if (cs == null) {
                logger.LogInformation("Running in iotedge context.");
            }
            else {
                logger.LogInformation("Running outside iotedge context.");
            }

            var client = await CreateAsync(cs, transportSetting).ConfigureAwait(false);
            var adapter = new ModuleClientAdapter(client, enableOutputRouting, metrics);

            // Configure
            client.OperationTimeoutInMilliseconds = (uint)timeout.TotalMilliseconds;
            client.SetConnectionStatusChangesHandler((s, r) =>
                adapter.OnConnectionStatusChange(deviceId, moduleId, onConnectionLost,
                    logger, s, r));
            if (retry != null) {
                client.SetRetryPolicy(retry);
            }
            client.ProductInfo = product;
            await client.OpenAsync().ConfigureAwait(false);
            return adapter;
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync() {
            if (IsClosed) {
                return;
            }
            _client.OperationTimeoutInMilliseconds = 3000;
            _client.SetRetryPolicy(new NoRetry());
            IsClosed = true;
            await _client.CloseAsync().ConfigureAwait(false);
        }

        /// <inheritdoc />
        public void Dispose() {
            IsClosed = true;
            _client?.Dispose();
        }

        /// <inheritdoc />
        public ITelemetryEvent CreateTelemetryEvent() {
            return new DeviceClientAdapter.DeviceMessage();
        }

        /// <inheritdoc />
        public async Task SendEventAsync(ITelemetryEvent message) {
            if (IsClosed) {
                return;
            }
            var msg = (DeviceClientAdapter.DeviceMessage)message;
            var messages = msg.AsMessages();
            try {
                if (!_enableOutputRouting || string.IsNullOrEmpty(msg.OutputName)) {
                    if (messages.Count == 1) {
                        await _client.SendEventAsync(messages[0]).ConfigureAwait(false);
                    }
                    else {
                        await _client.SendEventBatchAsync(messages).ConfigureAwait(false);
                    }
                }
                else {
                    if (messages.Count == 1) {
                        await _client.SendEventAsync(msg.OutputName, messages[0]).ConfigureAwait(false);
                    }
                    else {
                        await _client.SendEventBatchAsync(msg.OutputName, messages).ConfigureAwait(false);
                    }
                }
            }
            finally {
                foreach (var hubMessage in messages) {
                    hubMessage.Dispose();
                }
            }
        }

        /// <inheritdoc />
        public Task SetMethodHandlerAsync(
            MethodCallback methodHandler) {
            return _client.SetMethodDefaultHandlerAsync(methodHandler, null);
        }

        /// <inheritdoc />
        public Task SetDesiredPropertyUpdateCallbackAsync(
            DesiredPropertyUpdateCallback callback) {
            return _client.SetDesiredPropertyUpdateCallbackAsync(callback, null);
        }

        /// <inheritdoc />
        public Task<Twin> GetTwinAsync() {
            return _client.GetTwinAsync();
        }

        /// <inheritdoc />
        public async Task UpdateReportedPropertiesAsync(TwinCollection reportedProperties) {
            if (IsClosed) {
                return;
            }
            await _client.UpdateReportedPropertiesAsync(reportedProperties).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task<MethodResponse> InvokeMethodAsync(string deviceId, string moduleId,
            MethodRequest methodRequest, CancellationToken cancellationToken) {
            if (string.IsNullOrEmpty(moduleId)) {
                return _client.InvokeMethodAsync(deviceId, methodRequest, cancellationToken);
            }
            else {
                return _client.InvokeMethodAsync(deviceId, moduleId, methodRequest, cancellationToken);
            }
        }

        /// <summary>
        /// Handle status change event
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="onConnectionLost"></param>
        /// <param name="logger"></param>
        /// <param name="status"></param>
        /// <param name="reason"></param>
        private void OnConnectionStatusChange(string deviceId, string moduleId,
            Action onConnectionLost, ILogger logger, ConnectionStatus status,
            ConnectionStatusChangeReason reason) {
            _status = status;
            _reason = reason;
            if (status == ConnectionStatus.Connected) {
                logger.LogInformation("{Counter}: Module {DeviceId}_{ModuleId} reconnected " +
                    "due to {Reason}.", _reconnectCounter, deviceId, moduleId, reason);
                _reconnectCounter++;
                return;
            }
            logger.LogInformation("{Counter}: Module {DeviceId}_{ModuleId} disconnected " +
                "due to {Reason} - now {Status}...", _reconnectCounter, deviceId, moduleId,
                    reason, status);
            if (IsClosed) {
                // Already closed - nothing to do
                return;
            }
            if (status == ConnectionStatus.Disconnected ||
                status == ConnectionStatus.Disabled) {
                // Force
                IsClosed = true;
                onConnectionLost?.Invoke();
            }
        }

        /// <summary>
        /// Helper to create module client
        /// </summary>
        /// <param name="cs"></param>
        /// <param name="transportSetting"></param>
        /// <returns></returns>
        private static async Task<ModuleClient> CreateAsync(IotHubConnectionStringBuilder cs,
            ITransportSettings transportSetting) {
            if (transportSetting == null) {
                if (cs == null) {
                    return await ModuleClient.CreateFromEnvironmentAsync().ConfigureAwait(false);
                }
                return ModuleClient.CreateFromConnectionString(cs.ToString());
            }
            var ts = new ITransportSettings[] { transportSetting };
            if (cs == null) {
                return await ModuleClient.CreateFromEnvironmentAsync(ts).ConfigureAwait(false);
            }
            return ModuleClient.CreateFromConnectionString(cs.ToString(), ts);
        }

        /// <summary>
        /// Create observable metrics
        /// </summary>
        /// <param name="metrics"></param>
        private ModuleClientAdapter(IMetricsContext metrics) {
            Diagnostics.Meter.CreateObservableCounter("iiot_edge_reconnected",
                () => new Measurement<int>(_reconnectCounter, metrics.TagList), "times",
                "Device client reconnected count.");
            Diagnostics.Meter.CreateObservableGauge("iiot_edge_connection_status",
                () => new Measurement<int>((int)_status, metrics.TagList), "status",
                "Device client disconnected.");
            Diagnostics.Meter.CreateObservableGauge("iiot_edge_connection_reason",
                () => new Measurement<int>((int)_reason, metrics.TagList), "reason",
                "Device client disconnected.");
        }

        private ConnectionStatus _status;
        private ConnectionStatusChangeReason _reason;
        private readonly ModuleClient _client;
        private readonly bool _enableOutputRouting;
        private int _reconnectCounter;
    }
}
