// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client.MqttClient {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Common;
    using Microsoft.Azure.Devices.Shared;
    using MQTTnet;
    using MQTTnet.Client;
    using MQTTnet.Extensions.ManagedClient;
    using MQTTnet.Formatter;
    using MQTTnet.Packets;
    using MQTTnet.Protocol;
    using Newtonsoft.Json;
    using Prometheus;
    using Serilog;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.Metrics;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Runtime.InteropServices;
    using System.Security.Authentication;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// MQTT client adapter implementation
    /// </summary>
    public sealed class MqttClientAdapter : IClient {

        /// <inheritdoc />
        public int MaxEventBufferSize => int.MaxValue;

        /// <summary>
        /// Whether the client is closed
        /// </summary>
        public bool IsClosed { get; private set; }

        /// <summary>
        /// Whether the client is connected
        /// </summary>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// Whether the client is connected
        /// </summary>
        public bool IsMqttv5 => _protocolVersion == MqttProtocolVersion.V500;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="client">MQTT server client.</param>
        /// <param name="deviceId">Id of the device.</param>
        /// <param name="timeout">Timeout used for operations.</param>
        /// <param name="protocolVersion">Use mqtt v5 or 311</param>
        /// <param name="qualityOfServiceLevel">Quality of service level to use for MQTT messages.</param>
        /// <param name="telemetryTopicTemplate">A template to build Topics.</param>
        /// <param name="logger">Logger used for operations</param>
        /// <param name="metrics"></param>
        private MqttClientAdapter(IManagedMqttClient client, string deviceId, TimeSpan timeout,
            MqttProtocolVersion protocolVersion, MqttQualityOfServiceLevel qualityOfServiceLevel,
            string telemetryTopicTemplate, ILogger logger, IMetricsContext metrics)
            : this(metrics ?? throw new ArgumentNullException(nameof(metrics))) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _deviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
            _timeout = timeout;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _protocolVersion = protocolVersion;
            _qualityOfServiceLevel = qualityOfServiceLevel;
            _methodTopicRoot = "$iothub/methods";
            _twinTopicRoot = "$iothub/twin";

            if (!string.IsNullOrWhiteSpace(telemetryTopicTemplate)) {
                if (!telemetryTopicTemplate.Contains(kOutputNamePlaceholder)) {
                    if (telemetryTopicTemplate.EndsWith('/')) {
                        _telemetryTopicTemplate =
                            $"{telemetryTopicTemplate}{kOutputNamePlaceholder}/";
                    }
                    else {
                        _telemetryTopicTemplate =
                            $"{telemetryTopicTemplate}/{kOutputNamePlaceholder}";
                    }
                }
                else {
                    _telemetryTopicTemplate = telemetryTopicTemplate;
                }
                _methodTopicRoot = _telemetryTopicTemplate
                    .Replace(kDeviceIdTemplatePlaceholder, _deviceId)
                    .Replace(kOutputNamePlaceholder, "methods")
                    .Replace("//", "/")
                    ;
                _twinTopicRoot = _telemetryTopicTemplate
                    .Replace(kDeviceIdTemplatePlaceholder, _deviceId)
                    .Replace(kOutputNamePlaceholder, "twin")
                    .Replace("//", "/")
                    ;
            }
        }

        /// <summary>
        /// Create and connect an instance of the client adapter.
        /// </summary>
        /// <param name="client">MQTT server client.</param>
        /// <param name="cs">Connection string for the MQTT server.</param>
        /// <param name="deviceId">Id of the device.</param>
        /// <param name="telemetryTopicTemplate">Telemetry topic template.</param>
        /// <param name="timeout">Timeout used for operations.</param>
        /// <param name="logger">Logger used for operations</param>
        /// <param name="metrics"></param>
        /// <returns></returns>
        public static async Task<IClient> CreateAsync(IManagedMqttClient client,
            MqttClientConnectionStringBuilder cs, string deviceId, string telemetryTopicTemplate,
            TimeSpan timeout, ILogger logger, IMetricsContext metrics) {
            var options = new MqttClientOptionsBuilder()
                .WithClientId(string.IsNullOrEmpty(cs.DeviceId) ? Guid.NewGuid().ToString() : cs.DeviceId)
                .WithTcpServer(tcpOptions => {
                    tcpOptions.Server = cs.HostName;
                    tcpOptions.Port = cs.Port;

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                        tcpOptions.BufferSize = 64 * 1024;
                    }
                });

            if (!string.IsNullOrWhiteSpace(cs.Username) && !string.IsNullOrWhiteSpace(cs.Password)) {
                options = options.WithCredentials(cs.Username, cs.Password);
            }

            var tlsOptions = new MqttClientOptionsBuilderTlsParameters {
                UseTls = cs.UsingIoTHub,
                SslProtocol = SslProtocols.Tls12,
                IgnoreCertificateRevocationErrors = true
            };

            if (cs.UsingX509Cert) {
                tlsOptions.Certificates = new List<X509Certificate> { cs.X509Cert };
            }
            options = options.WithTls(tlsOptions);

            if (cs.Protocol != MqttProtocolVersion.Unknown) {
                options = options.WithProtocolVersion(cs.Protocol);
            }

            var adapter = new MqttClientAdapter(client, deviceId, timeout, cs.Protocol,
                qualityOfServiceLevel: MqttQualityOfServiceLevel.AtLeastOnce,
                telemetryTopicTemplate: cs.UsingIoTHub ? null : telemetryTopicTemplate,
                logger: logger, metrics: metrics);

            client.ConnectedAsync += adapter.OnConnected;
            client.ConnectingFailedAsync += adapter.OnConnectingFailed;
            client.ConnectionStateChangedAsync += adapter.OnConnectionStateChanged;
            client.SynchronizingSubscriptionsFailedAsync += adapter.SynchronizingSubscriptionsFailed;
            client.DisconnectedAsync += adapter.OnDisconnected;
            client.ApplicationMessageReceivedAsync += adapter.OnApplicationMessageReceivedHandler;

            var managedOptions = new ManagedMqttClientOptionsBuilder()
                .WithClientOptions(options.Build())
                .WithAutoReconnectDelay(TimeSpan.FromMilliseconds(300))
                .WithPendingMessagesOverflowStrategy(
                    MQTTnet.Server.MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage)
                ;
            if (cs.UsingStateFile) {
                managedOptions = managedOptions.WithStorage(new ManagedMqttClientStorage(cs.StateFile, logger));
            }

            await adapter.StartAsync(managedOptions.Build());
            return adapter;
        }

        /// <summary>
        /// Create and connect an instance of the client adapter.
        /// </summary>
        /// <param name="cs">Connection string for the MQTT server.</param>
        /// <param name="deviceId">Id of the device.</param>
        /// <param name="telemetryTopicTemplate">Telemetry topic template.</param>
        /// <param name="timeout">Timeout used for operations.</param>
        /// <param name="logger">Logger used for operations</param>
        /// <param name="metrics"></param>
        /// <returns></returns>
        public static Task<IClient> CreateAsync(MqttClientConnectionStringBuilder cs,
            string deviceId, string telemetryTopicTemplate, TimeSpan timeout, ILogger logger,
            IMetricsContext metrics) {
            var client = new MqttFactory().CreateManagedMqttClient();
            return CreateAsync(client, cs, deviceId, telemetryTopicTemplate, timeout, logger,
                metrics);
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync() {
            if (IsClosed) {
                return;
            }
            IsClosed = true;
            await _client.StopAsync();
        }

        /// <inheritdoc />
        public ITelemetryEvent CreateTelemetryEvent() {
            return new MqttClientAdapterMessage(this);
        }

        /// <inheritdoc />
        public async Task SendEventAsync(ITelemetryEvent message) {
            if (IsClosed) {
                return;
            }
            var msg = (MqttClientAdapterMessage)message;
            var topic = msg.Topic;
            foreach (var body in msg.Buffers) {
                if (body != null) {
                    await InternalSendEventAsync(topic, body, msg.ContentType, msg.Retain,
                        msg.Ttl, msg.UserProperties);
                }
            }
        }

        /// <inheritdoc />
        public Task SetMethodHandlerAsync(MethodCallback methodHandler) {
            _defaultMethodCallback = methodHandler;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdateCallback callback) {
            _desiredPropertyUpdateCallback = callback;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task<Twin> GetTwinAsync() {
            if (IsClosed || _telemetryTopicTemplate != null) {
                return null;
            }

            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(_timeout);

            // Signal that the response should be saved by setting the key.
            var requestId = Guid.NewGuid().ToString();
            _responses[requestId] = null;

            Twin result = null;
            try {
                // Publish message and wait for response to come back. The thread may be unblocked by other
                // simultaneous calls, so wait again if needed.
                await InternalSendEventAsync($"{_twinTopicRoot}/GET/?$rid={requestId}");
                while (!cancellationTokenSource.Token.IsCancellationRequested) {
                    _responseHandle.WaitOne(_timeout);
                    _responseHandle.Reset();
                    if (_responses[requestId] != null) {
                        result = new Twin {
                            Properties = JsonConvert.DeserializeObject<TwinProperties>(
                                Encoding.UTF8.GetString(_responses[requestId].Payload)),
                        };
                        break;
                    }
                }

                if (result == null) {
                    cancellationTokenSource.Token.ThrowIfCancellationRequested();
                }
            }
            catch (TaskCanceledException) {
                _logger.Error("Failed to get twin due to timeout: {Timeout}", _timeout);
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to get twin.");
            }
            _responses.Remove(requestId, out _);
            return result;
        }

        /// <inheritdoc />
        public async Task UpdateReportedPropertiesAsync(TwinCollection reportedProperties) {
            if (IsClosed) {
                return;
            }
            var payload = Encoding.UTF8.GetBytes(reportedProperties.ToJson());
            await InternalSendEventAsync(
                $"{_twinTopicRoot}/PATCH/properties/reported/?$rid=patch_temp", payload);
        }

        /// <inheritdoc />
        public Task<MethodResponse> InvokeMethodAsync(string deviceId, string moduleId,
            MethodRequest methodRequest, CancellationToken cancellationToken) {
            throw new NotSupportedException("MQTT client does not support methods");
        }

        /// <inheritdoc />
        public void Dispose() {
            IsClosed = true;
            _client?.Dispose();
        }

        /// <summary>
        /// Send event as MQTT message with configured properties for the client.
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="topic"></param>
        /// <param name="retain"></param>
        /// <param name="ttl"></param>
        /// <param name="properties"></param>
        /// <param name="correlationData"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        /// <exception cref="MessageTooLargeException"></exception>
        private Task InternalSendEventAsync(string topic, byte[] payload = null,
            string contentType = null, bool retain = false, TimeSpan? ttl = null,
            Dictionary<string, string> properties = null, byte[] correlationData = null) {

            _logger.Debug("Publishing {ByteCount} bytes to {Topic}",
                payload != null ? payload.Length : 0, topic);

            // Check topic length.
            var topicLength = Encoding.UTF8.GetByteCount(topic);
            if (topicLength > kMaxTopicLength) {
                throw new MessageTooLargeException(
                    $"Topic for MQTT message cannot be larger than {kMaxTopicLength} bytes, " +
                    $"but current length is {topicLength}. The list of message.properties " +
                    $"and/or message.systemProperties is likely too long. Please use AMQP or HTTP.");
            }
            try {
                // Build MQTT message.
                var mqttApplicationMessageBuilder = new MqttApplicationMessageBuilder()
                    .WithQualityOfServiceLevel(_qualityOfServiceLevel)
                    .WithContentType(contentType ?? kContentType)
                    .WithTopic(topic)
                    .WithRetainFlag(retain)
                    ;

                if (correlationData != null) {
                    mqttApplicationMessageBuilder.WithCorrelationData(correlationData);
                }
                if (payload != null) {
                    mqttApplicationMessageBuilder = mqttApplicationMessageBuilder.WithPayload(payload);
                }
                if (ttl.HasValue && ttl.Value > TimeSpan.Zero) {
                    mqttApplicationMessageBuilder = mqttApplicationMessageBuilder
                        .WithMessageExpiryInterval((uint)ttl.Value.TotalSeconds);
                }
                if (properties != null) {
                    foreach (var userProperty in properties) {
                        mqttApplicationMessageBuilder.WithUserProperty(userProperty.Key, userProperty.Value);
                    }
                }
                var mqttmessage = mqttApplicationMessageBuilder.Build();
                return _client.EnqueueAsync(mqttmessage);
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to publish MQTT message.");
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handler for when the internal MQTT client is connected.
        /// </summary>
        /// <param name="eventArgs">Event arguments.</param>
        /// <returns></returns>
        private Task OnConnected(MqttClientConnectedEventArgs eventArgs) {
            _logger.Information("{counter}: Device {deviceId} connected or reconnected",
                _reconnectCounter, _deviceId);
            _reconnectCounter++;
            IsConnected = true;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handler for when the subscription synchronizing failed
        /// </summary>
        /// <param name="eventArgs">Event arguments.</param>
        /// <returns></returns>
        private Task SynchronizingSubscriptionsFailed(ManagedProcessFailedEventArgs eventArgs) {
            _logger.Information("{counter}: Device {deviceId} subscription sync failed.",
                _reconnectCounter, _deviceId);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handler for when the connection state failed
        /// </summary>
        /// <param name="eventArgs">Event arguments.</param>
        /// <returns></returns>
        private Task OnConnectionStateChanged(EventArgs eventArgs) {
            _logger.Information("{counter}: Device {deviceId} connection state changed.",
                _reconnectCounter, _deviceId);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handler for when connecting failed
        /// </summary>
        /// <param name="eventArgs">Event arguments.</param>
        /// <returns></returns>
        private Task OnConnectingFailed(ConnectingFailedEventArgs eventArgs) {
            IsConnected = false;

            _logger.Information("{counter}: Device {deviceId} disconnected due to {reason}",
                 _reconnectCounter, _deviceId, eventArgs.ToString());

            // _onConnectionLost?.Invoke();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handler for when the internal MQTT client is disconnected.
        /// </summary>
        /// <param name="eventArgs">Event arguments.</param>
        /// <returns></returns>
        private Task OnDisconnected(MqttClientDisconnectedEventArgs eventArgs) {
            if (!IsConnected) {
                return Task.CompletedTask;
            }
            IsConnected = false;

            _logger.Information("{counter}: Device {deviceId} disconnected due to {reason}",
                 _reconnectCounter, _deviceId, eventArgs.Reason);

            // _onConnectionLost?.Invoke();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Subscribe to all required topics
        /// </summary>
        /// <returns></returns>
        private async Task StartAsync(ManagedMqttClientOptions options) {
            // Start the client which connects to the server in the background.
            await _client.StartAsync(options);

            // Now subscribe using managed client
            await _client.SubscribeAsync(new List<MqttTopicFilter> {
                new MqttTopicFilter {
                    Topic = $"{_twinTopicRoot}/res/#"
                },
                new MqttTopicFilter {
                    Topic = $"{_twinTopicRoot}/PATCH/properties/desired/#"
                },
                new MqttTopicFilter {
                    Topic = $"{_methodTopicRoot}/#"
                }
            });
        }

        /// <summary>
        /// Handler for when a response is received.
        /// </summary>
        /// <param name="eventArgs">Event arguments.</param>
        /// <returns></returns>
        private Task OnTwinResponseAsync(MqttApplicationMessageReceivedEventArgs eventArgs) {

            // Only handling twin GET response for now.

            // Only store the response if a thread is waiting for it (indicated by key added).
            var requestId = eventArgs.ApplicationMessage.Topic.Substring(
                $"{_twinTopicRoot}/res/200/?$rid=".Length);
            if (_responses.ContainsKey(requestId)) {
                _responses[requestId] = eventArgs.ApplicationMessage;
            }

            // Unblock all threads waiting for responses.
            _responseHandle.Set();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handler for when desired properties are updated.
        /// </summary>
        /// <param name="eventArgs">Event arguments.</param>
        /// <returns></returns>
        private Task OnDesiredPropertiesUpdatedAsync(MqttApplicationMessageReceivedEventArgs eventArgs) {
            var twinCollection = JsonConvert.DeserializeObject<TwinCollection>(
                Encoding.UTF8.GetString(eventArgs.ApplicationMessage.Payload));
            _desiredPropertyUpdateCallback?.Invoke(twinCollection, null);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handler for when a method is called.
        /// </summary>
        /// <param name="eventArgs">Event arguments.</param>
        /// <returns></returns>
        private async Task OnMethodRequestAsync(MqttApplicationMessageReceivedEventArgs eventArgs) {
            // Parse topic.
            var components = eventArgs.ApplicationMessage.Topic.Split('/');

            string methodName;
            string requestId = null;
            if (IsMqttv5) {
                if (string.IsNullOrEmpty(eventArgs.ApplicationMessage.ResponseTopic)) {
                    // No response topic
                    return;
                }
                methodName = components[components.Length - 1];
            }
            else {
                if (components.Length < 2) {
                    return;
                }
                methodName = components[components.Length - 2];
                requestId = components[components.Length - 1].Substring("?$rid=".Length);
            }

            // Get callback and user context.
            var callback = _defaultMethodCallback;
            if (callback == null) {
                return;
            }

            // Invoke callback.
            var methodRequest = new MethodRequest(methodName, eventArgs.ApplicationMessage.Payload);
            try {
                var methodResponse = await callback.Invoke(methodRequest, null);
                var payload = methodResponse.Result != null && methodResponse.Result.Length > 0
                    ? methodResponse.Result : null;
                var statusCode = methodResponse.Status.ToString();
                if (IsMqttv5) {
                    await InternalSendEventAsync(eventArgs.ApplicationMessage.ResponseTopic, payload,
                        properties: new Dictionary<string, string> { [kStatusCodeKey] = statusCode },
                        correlationData: eventArgs.ApplicationMessage.CorrelationData);
                }
                else {
                    await InternalSendEventAsync(
                        $"{_methodTopicRoot}/res/{statusCode}/?$rid={requestId}", payload);
                }
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to call method \"{MethodName}\"", methodName);
                var statusCode = ((int)HttpStatusCode.InternalServerError).ToString();
                if (IsMqttv5) {
                    await InternalSendEventAsync(eventArgs.ApplicationMessage.ResponseTopic,
                        properties: new Dictionary<string, string> { [kStatusCodeKey] = statusCode },
                        correlationData: eventArgs.ApplicationMessage.CorrelationData);
                }
                else {
                    await InternalSendEventAsync(
                        $"{_methodTopicRoot}/res/{statusCode}/?$rid={requestId}");
                }
            }
        }

        /// <summary>
        /// Handler for when a method response was received
        /// </summary>
        /// <param name="eventArgs"></param>
        private Task OnMethodResponseAsync(MqttApplicationMessageReceivedEventArgs eventArgs) {
            // TODO: Support responses
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handler for when the MQTT client receives an application message.
        /// </summary>
        /// <param name="eventArgs">Event arguments.</param>
        /// <returns></returns>
        private Task OnApplicationMessageReceivedHandler(MqttApplicationMessageReceivedEventArgs eventArgs) {
            if (eventArgs.ProcessingFailed) {
                _logger.Warning("Failed to process MQTT message: {reasonCode}", eventArgs.ReasonCode);
                return Task.CompletedTask;
            }
            var topic = eventArgs.ApplicationMessage.Topic;
            try {
                if (topic.StartsWith($"{_methodTopicRoot}/res/", StringComparison.OrdinalIgnoreCase)) {
                    //
                    // Handle method responses for methods called on other components.
                    // All responses are diverted to topics starting with /res either through
                    // the 3.11 method of using $rid= or through response topics.
                    //
                    return OnMethodResponseAsync(eventArgs);
                }
                else if (topic.StartsWith($"{_methodTopicRoot}/", StringComparison.OrdinalIgnoreCase)) {
                    //
                    // typically should start with method topic root and /POST/ element but
                    // we allow any topic path components in between it and the method.
                    //
                    return OnMethodRequestAsync(eventArgs);
                }
                else if (topic.StartsWith($"{_twinTopicRoot}/res/200/?$rid=",
                    StringComparison.OrdinalIgnoreCase)) {
                    return OnTwinResponseAsync(eventArgs);
                }
                else if (topic.StartsWith($"{_twinTopicRoot}/PATCH/properties/desired/?$version=",
                    StringComparison.OrdinalIgnoreCase)) {
                    return OnDesiredPropertiesUpdatedAsync(eventArgs);
                }
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to process MQTT message.");
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Message wrapper
        /// </summary>
        internal sealed class MqttClientAdapterMessage : ITelemetryEvent {

            /// <summary>
            /// User properties
            /// </summary>
            internal Dictionary<string, string> UserProperties
                => _outer._telemetryTopicTemplate == null ? null : _userProperties;

            /// <summary>
            /// Topic
            /// </summary>
            internal string Topic {
                get {
                    // Build merged dictionary of properties to serialize in topic.
                    if (_outer._telemetryTopicTemplate == null) {
                        var topic = $"devices/{_outer._deviceId}/messages/events/";
                        if (_userProperties.Count > 0) {
                            topic += UrlEncodedDictionarySerializer.Serialize(_userProperties) + kSegmentSeparator;
                        }
                        return topic;
                    }
                    else {
                        return _outer._telemetryTopicTemplate
                            .Replace(kDeviceIdTemplatePlaceholder, _outer._deviceId)
                            .Replace(kOutputNamePlaceholder, OutputName ?? string.Empty)
                            .Replace("//", "/")
                            ;
                    }
                }
            }

            /// <inheritdoc/>
            public DateTime Timestamp { get; set; }

            /// <inheritdoc/>
            public string ContentType {
                get {
                    return _userProperties[kContentTypePropertyName];
                }
                set {
                    if (!string.IsNullOrWhiteSpace(value)) {
                        _userProperties[kContentTypePropertyName] = value;
                        if (_outer._telemetryTopicTemplate == null) {
                            _userProperties[SystemProperties.MessageSchema] = value;
                        }
                    }
                }
            }

            /// <inheritdoc/>
            public string ContentEncoding {
                get {
                    return _userProperties[kContentEncodingPropertyName];
                }
                set {
                    if (!string.IsNullOrWhiteSpace(value)) {
                        _userProperties[kContentEncodingPropertyName] = value;
                        if (_outer._telemetryTopicTemplate == null) {
                            _userProperties[CommonProperties.ContentEncoding] = value;
                        }
                    }
                }
            }

            /// <inheritdoc/>
            public string MessageSchema {
                get {
                    return _userProperties[CommonProperties.EventSchemaType];
                }
                set {
                    if (!string.IsNullOrWhiteSpace(value)) {
                        _userProperties[CommonProperties.EventSchemaType] = value;
                    }
                }
            }

            /// <inheritdoc/>
            public string RoutingInfo {
                get {
                    return _userProperties[CommonProperties.RoutingInfo];
                }
                set {
                    if (!string.IsNullOrWhiteSpace(value)) {
                        _userProperties[CommonProperties.RoutingInfo] = value;
                    }
                }
            }

            /// <inheritdoc/>
            public string DeviceId {
                get {
                    return _userProperties[CommonProperties.DeviceId];
                }
                set {
                    if (!string.IsNullOrWhiteSpace(value)) {
                        _userProperties[CommonProperties.DeviceId] = value;
                    }
                }
            }

            /// <inheritdoc/>
            public string ModuleId {
                get {
                    return _userProperties[CommonProperties.ModuleId];
                }
                set {
                    if (!string.IsNullOrWhiteSpace(value)) {
                        _userProperties[CommonProperties.ModuleId] = value;
                    }
                }
            }

            /// <inheritdoc/>
            public string OutputName { get; set; }
            /// <inheritdoc/>
            public bool Retain { get; set; }
            /// <inheritdoc/>
            public TimeSpan Ttl { get; set; }
            /// <inheritdoc/>
            public IReadOnlyList<byte[]> Buffers { get; set; }

            /// <inheritdoc/>
            public void Dispose() {
                _userProperties.Clear();
            }

            /// <summary>
            /// Create message
            /// </summary>
            /// <param name="outer"></param>
            internal MqttClientAdapterMessage(MqttClientAdapter outer) {
                _outer = outer;
            }

            private readonly MqttClientAdapter _outer;
            private readonly Dictionary<string, string> _userProperties = new Dictionary<string, string>();
        }

        /// <summary>
        /// Create observable metrics
        /// </summary>
        /// <param name="metrics"></param>
        private MqttClientAdapter(IMetricsContext metrics) {
            Diagnostics.Meter.CreateObservableCounter("iiot_edge_mqtt_reconnect_count",
                () => new Measurement<int>(_reconnectCounter, metrics.TagList), "times",
                "MQTT client reconnected count.");
            Diagnostics.Meter.CreateObservableGauge("iiot_edge_mqtt_disconnected",
                () => new Measurement<int>(IsConnected ? 0 : 1, metrics.TagList), "status",
                "MQTT client disconnected.");
        }

        private readonly IManagedMqttClient _client;
        private readonly MqttQualityOfServiceLevel _qualityOfServiceLevel;
        private readonly string _methodTopicRoot;
        private readonly string _twinTopicRoot;
        private readonly string _telemetryTopicTemplate;

        private const int kMaxTopicLength = 0xffff;
        private const string kSegmentSeparator = "/";
        private const string kContentEncodingPropertyName = "iothub-content-encoding";
        private const string kContentTypePropertyName = "iothub-content-type";
        private const string kContentType = "application/json";
        private const string kDeviceIdTemplatePlaceholder = "{device_id}";
        private const string kOutputNamePlaceholder = "{output_name}";
        private const string kStatusCodeKey = "StatusCode";

        private readonly string _deviceId;
        private readonly TimeSpan _timeout;
        private readonly ILogger _logger;
        private readonly MqttProtocolVersion _protocolVersion;
        private ManualResetEvent _responseHandle = new ManualResetEvent(false);
        private ConcurrentDictionary<string, MqttApplicationMessage> _responses
            = new ConcurrentDictionary<string, MqttApplicationMessage>();
        private DesiredPropertyUpdateCallback _desiredPropertyUpdateCallback;
        private MethodCallback _defaultMethodCallback;
        private int _reconnectCounter;
    }
}
