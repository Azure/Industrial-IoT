// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client {
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Shared;
    using Prometheus;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Adapts device client to interface
    /// </summary>
    public sealed class DeviceClientAdapter : IClient {

        /// <summary>
        /// Whether the client is closed
        /// </summary>
        public bool IsClosed { get; internal set; }

        /// <inheritdoc />
        public int MaxMessageSize => 256 * 1024;

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="client"></param>
        /// <param name="logger"></param>
        internal DeviceClientAdapter(DeviceClient client, ILogger logger) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Factory
        /// </summary>
        /// <param name="product"></param>
        /// <param name="cs"></param>
        /// <param name="deviceId"></param>
        /// <param name="transportSetting"></param>
        /// <param name="timeout"></param>
        /// <param name="retry"></param>
        /// <param name="onConnectionLost"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static async Task<IClient> CreateAsync(string product,
            IotHubConnectionStringBuilder cs, string deviceId,
            ITransportSettings transportSetting, TimeSpan timeout,
            IRetryPolicy retry, Action onConnectionLost, ILogger logger) {
            var client = Create(cs, transportSetting);
            var adapter = new DeviceClientAdapter(client, logger);

            // Configure
            client.OperationTimeoutInMilliseconds = (uint)timeout.TotalMilliseconds;
            client.SetConnectionStatusChangesHandler((s, r) =>
                adapter.OnConnectionStatusChange(deviceId, onConnectionLost, logger, s, r));
            if (retry != null) {
                client.SetRetryPolicy(retry);
            }
            client.ProductInfo = product;

            await client.OpenAsync();
            return adapter;
        }

        /// <inheritdoc />
        public async Task CloseAsync() {
            if (IsClosed) {
                return;
            }
            _client.OperationTimeoutInMilliseconds = 3000;
            _client.SetRetryPolicy(new NoRetry());
            IsClosed = true;
            await _client.CloseAsync();
        }

        /// <inheritdoc />
        public async Task SendEventAsync(Message message) {
            if (IsClosed) {
                return;
            }
            await _client.SendEventAsync(message);
        }

        /// <inheritdoc />
        public async Task SendEventAsync(string outputName, Message message) {
			if (IsClosed) {
            	return;
            }
            _logger.Debug("DeviceClientAdapter does not support output routing. Falling back to regular SendEventAsync()");
            await _client.SendEventAsync(message);
        }

        /// <inheritdoc />
        public async Task SendEventBatchAsync(IEnumerable<Message> messages) {
            if (IsClosed) {
                return;
            }
            await _client.SendEventBatchAsync(messages);
        }

        /// <inheritdoc />
        public async Task SendEventBatchAsync(string outputName, IEnumerable<Message> messages) {
            if (IsClosed) {
                return;
            }
            _logger.Debug("DeviceClientAdapter does not support output routing. Falling back to regular SendEventBatchAsync()");
            await _client.SendEventBatchAsync(messages);
        }

        /// <inheritdoc />
        public Task SetMethodHandlerAsync(string methodName,
            MethodCallback methodHandler, object userContext) {
            return _client.SetMethodHandlerAsync(methodName, methodHandler, userContext);
        }

        /// <inheritdoc />
        public Task SetMethodDefaultHandlerAsync(
            MethodCallback methodHandler, object userContext) {
            return _client.SetMethodDefaultHandlerAsync(methodHandler, userContext);
        }

        /// <inheritdoc />
        public Task SetDesiredPropertyUpdateCallbackAsync(
            DesiredPropertyUpdateCallback callback, object userContext) {
            return _client.SetDesiredPropertyUpdateCallbackAsync(callback, userContext);
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
            await _client.UpdateReportedPropertiesAsync(reportedProperties);
        }

        /// <inheritdoc />
        public async Task UploadToBlobAsync(string blobName, Stream source) {
            if (IsClosed) {
                return;
            }
#pragma warning disable CS0618 // Type or member is obsolete
            await _client.UploadToBlobAsync(blobName, source);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        /// <inheritdoc />
        public Task<MethodResponse> InvokeMethodAsync(string deviceId, string moduleId,
            MethodRequest methodRequest, CancellationToken cancellationToken) {
            throw new NotSupportedException("Device client does not support methods");
        }

        /// <inheritdoc />
        public Task<MethodResponse> InvokeMethodAsync(string deviceId,
            MethodRequest methodRequest, CancellationToken cancellationToken) {
            throw new NotSupportedException("Device client does not support methods");
        }

        /// <inheritdoc />
        public void Dispose() {
            IsClosed = true;
            _client?.Dispose();
        }

        /// <summary>
        /// Handle status change event
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="onConnectionLost"></param>
        /// <param name="logger"></param>
        /// <param name="status"></param>
        /// <param name="reason"></param>
        private void OnConnectionStatusChange(string deviceId,
            Action onConnectionLost, ILogger logger, ConnectionStatus status,
            ConnectionStatusChangeReason reason) {

            if (status == ConnectionStatus.Connected) {
                logger.Information("{counter}: Device {deviceId} reconnected " +
                    "due to {reason}.", _reconnectCounter, deviceId, reason);
                kReconnectionStatus.WithLabels(deviceId).Set(_reconnectCounter);
                _reconnectCounter++;
                return;
            }
            logger.Information("{counter}: Device {deviceId} disconnected " +
                "due to {reason} - now {status}...", _reconnectCounter, deviceId,
                    reason, status);
            kDisconnectionStatus.WithLabels(deviceId).Set(_reconnectCounter);
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
        /// Helper to create device client
        /// </summary>
        /// <param name="cs"></param>
        /// <param name="transportSetting"></param>
        /// <returns></returns>
        private static DeviceClient Create(IotHubConnectionStringBuilder cs,
            ITransportSettings transportSetting) {
            if (cs == null) {
                throw new ArgumentNullException(nameof(cs));
            }
            if (transportSetting != null) {
                return DeviceClient.CreateFromConnectionString(cs.ToString(),
                    new ITransportSettings[] { transportSetting });
            }
            return DeviceClient.CreateFromConnectionString(cs.ToString());
        }

        private readonly DeviceClient _client;
        private readonly ILogger _logger;

        private int _reconnectCounter;
        private static readonly Gauge kReconnectionStatus = Metrics
            .CreateGauge("iiot_edge_device_reconnected", "reconnected count",
                new GaugeConfiguration {
                    LabelNames = new[] { "device" }
                });
        private static readonly Gauge kDisconnectionStatus = Metrics
            .CreateGauge("iiot_edge_device_disconnected", "disconnected count",
                new GaugeConfiguration {
                    LabelNames = new[] { "device" }
                });
    }
}
