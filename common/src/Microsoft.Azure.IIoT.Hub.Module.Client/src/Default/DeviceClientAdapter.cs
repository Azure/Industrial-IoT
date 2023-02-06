// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client {
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Shared;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Messaging;
    using Prometheus;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Metrics;
    using System.Linq;
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
        public int MaxEventBufferSize => 256 * 1024;

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="client"></param>
        /// <param name="logger"></param>
        /// <param name="metrics"></param>
        internal DeviceClientAdapter(DeviceClient client, ILogger logger,
            IMetricsContext metrics)
            : this(metrics ?? throw new ArgumentNullException(nameof(metrics))) {
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
        /// <param name="metrics"></param>
        /// <returns></returns>
        public static async Task<IClient> CreateAsync(string product,
            IotHubConnectionStringBuilder cs, string deviceId,
            ITransportSettings transportSetting, TimeSpan timeout,
            IRetryPolicy retry, Action onConnectionLost, ILogger logger,
            IMetricsContext metrics) {
            var client = Create(cs, transportSetting);
            var adapter = new DeviceClientAdapter(client, logger, metrics);

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
        public async ValueTask DisposeAsync() {
            if (IsClosed) {
                return;
            }
            _client.OperationTimeoutInMilliseconds = 3000;
            _client.SetRetryPolicy(new NoRetry());
            IsClosed = true;
            await _client.CloseAsync();
        }

        /// <inheritdoc />
        public ITelemetryEvent CreateTelemetryEvent() {
            return new DeviceMessage();
        }

        /// <inheritdoc />
        public async Task SendEventAsync(ITelemetryEvent message) {
            if (IsClosed) {
                return;
            }
            var messages = ((DeviceMessage)message).AsMessages();
            try {
                if (messages.Count == 1) {
                    await _client.SendEventAsync(messages[0]);
                }
                await _client.SendEventBatchAsync(messages);
            }
            finally {
                foreach (var hubMessage in messages) {
                    hubMessage.Dispose();
                }
            }
        }

        /// <inheritdoc />
        public Task SetMethodHandlerAsync(MethodCallback methodHandler) {
            return _client.SetMethodDefaultHandlerAsync(methodHandler, null);
        }

        /// <inheritdoc />
        public Task SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdateCallback callback) {
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
            await _client.UpdateReportedPropertiesAsync(reportedProperties);
        }

        /// <inheritdoc />
        public Task<MethodResponse> InvokeMethodAsync(string deviceId, string moduleId,
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

            _status = status;
            _reason = reason;
            if (status == ConnectionStatus.Connected) {
                logger.Information("{counter}: Device {deviceId} reconnected " +
                    "due to {reason}.", _reconnectCounter, deviceId, reason);
                _reconnectCounter++;
                return;
            }
            logger.Information("{counter}: Device {deviceId} disconnected " +
                "due to {reason} - now {status}...", _reconnectCounter, deviceId,
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


        /// <summary>
        /// Message wrapper
        /// </summary>
        internal sealed class DeviceMessage : ITelemetryEvent {

            /// <summary>
            /// Build message
            /// </summary>
            internal IReadOnlyList<Message> AsMessages() {
                return Buffers
                    .Where(b => b != null)
                    .Select(m => _template.CloneWithBody(m))
                    .ToList();
            }

            /// <inheritdoc/>
            public DateTime Timestamp { get; set; }

            /// <inheritdoc/>
            public string ContentType {
                get {
                    return _template.ContentType;
                }
                set {
                    if (!string.IsNullOrWhiteSpace(value)) {
                        _template.ContentType = value;
                        _template.Properties.AddOrUpdate(SystemProperties.MessageSchema, value);
                    }
                }
            }

            /// <inheritdoc/>
            public string ContentEncoding {
                get {
                    return _template.ContentEncoding;
                }
                set {
                    if (!string.IsNullOrWhiteSpace(value)) {
                        _template.ContentEncoding = value;
                        _template.Properties.AddOrUpdate(CommonProperties.ContentEncoding, value);
                    }
                }
            }

            /// <inheritdoc/>
            public string MessageSchema {
                get {
                    if (_template.Properties.TryGetValue(CommonProperties.EventSchemaType, out var value)) {
                        return value;
                    }
                    return null;
                }
                set {
                    if (!string.IsNullOrWhiteSpace(value)) {
                        _template.Properties.AddOrUpdate(CommonProperties.EventSchemaType, value);
                    }
                }
            }

            /// <inheritdoc/>
            public string RoutingInfo {
                get {
                    if (_template.Properties.TryGetValue(CommonProperties.RoutingInfo, out var value)) {
                        return value;
                    }
                    return null;
                }
                set {
                    if (!string.IsNullOrWhiteSpace(value)) {
                        _template.Properties.AddOrUpdate(CommonProperties.RoutingInfo, value);
                    }
                }
            }

            /// <inheritdoc/>
            public string DeviceId {
                get {
                    if (_template.Properties.TryGetValue(CommonProperties.DeviceId, out var value)) {
                        return value;
                    }
                    return null;
                }
                set {
                    if (!string.IsNullOrWhiteSpace(value)) {
                        _template.Properties.AddOrUpdate(CommonProperties.DeviceId, value);
                    }
                }
            }

            /// <inheritdoc/>
            public string ModuleId {
                get {
                    if (_template.Properties.TryGetValue(CommonProperties.ModuleId, out var value)) {
                        return value;
                    }
                    return null;
                }
                set {
                    if (!string.IsNullOrWhiteSpace(value)) {
                        _template.Properties.AddOrUpdate(CommonProperties.ModuleId, value);
                    }
                }
            }

            /// <inheritdoc/>
            public IReadOnlyList<byte[]> Buffers { get; set; }

            /// <inheritdoc/>
            public string OutputName { get; set; }
            /// <inheritdoc/>
            public bool Retain { get; set; }
            /// <inheritdoc/>
            public TimeSpan Ttl { get; set; }

            /// <inheritdoc/>
            public void Dispose() {
                // TODO: Return to pool
                _template.Dispose();
            }

            Message _template = new Message();
        }

        /// <summary>
        /// Create observable metrics
        /// </summary>
        /// <param name="metrics"></param>
        private DeviceClientAdapter(IMetricsContext metrics) {
            Diagnostics.Meter.CreateObservableCounter("iiot_edge_device_reconnected",
                () => new Measurement<int>(_reconnectCounter, metrics.TagList), "times",
                "Device client reconnected count.");
            Diagnostics.Meter.CreateObservableGauge("iiot_edge_device_connection_status",
                () => new Measurement<int>((int)_status, metrics.TagList), "status",
                "Device client disconnected.");
            Diagnostics.Meter.CreateObservableGauge("iiot_edge_device_connection_reason",
                () => new Measurement<int>((int)_reason, metrics.TagList), "reason",
                "Device client disconnected.");
        }

        private ConnectionStatus _status;
        private ConnectionStatusChangeReason _reason;
        private readonly DeviceClient _client;
        private readonly ILogger _logger;
        private int _reconnectCounter;
    }
}
