// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client.MqttClient {
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Common;
    using Microsoft.Azure.Devices.Shared;
    using Microsoft.Azure.IIoT.Exceptions;
    using MQTTnet;
    using MQTTnet.Client;
    using MQTTnet.Client.Connecting;
    using MQTTnet.Client.Disconnecting;
    using MQTTnet.Client.Options;
    using MQTTnet.Extensions.ManagedClient;
    using MQTTnet.Formatter;
    using MQTTnet.Protocol;
    using Newtonsoft.Json;
    using Prometheus;
    using Serilog;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
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

        /// <summary>
        /// Whether the client is closed
        /// </summary>
        public bool IsClosed { get; private set; }

        /// <inheritdoc />
        public int MaxMessageSize => int.MaxValue;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="client">MQTT server client.</param>
        /// <param name="deviceId">Id of the device.</param>
        /// <param name="timeout">Timeout used for operations.</param>
        /// <param name="onConnectionLost">Handler for when the MQTT server connection is lost.</param>
        /// <param name="messageExpiryInterval">Period of time (seconds) for the broker to store the message for any subscribers that are not yet connected</param>
        /// <param name="qualityOfServiceLevel">Quality of service level to use for MQTT messages.</param>
        /// <param name="useIoTHubTopics">A flag determining whether IoT Hub compatible Topics shall be used.</param>
        /// <param name="telemetryTopicTemplate">A template to build Topics. Valid Placeholders are : {device_id}</param>
        /// <param name="logger">Logger used for operations</param>
        private MqttClientAdapter(
            IManagedMqttClient client,
            string deviceId,
            TimeSpan timeout,
            Action onConnectionLost,
            uint? messageExpiryInterval,
            MqttQualityOfServiceLevel qualityOfServiceLevel,
            bool useIoTHubTopics,
            string telemetryTopicTemplate,
            ILogger logger) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _deviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
            _timeout = timeout;
            _onConnectionLost = onConnectionLost ?? throw new ArgumentNullException(nameof(onConnectionLost));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messageExpiryInterval = messageExpiryInterval;
            _qualityOfServiceLevel = qualityOfServiceLevel;
            _useIoTHubTopics = useIoTHubTopics;
            _telemetryTopicTemplate = telemetryTopicTemplate;
        }

        /// <summary>
        /// Create and connect an instance of the client adapter.
        /// </summary>
        /// <param name="client">MQTT server client.</param>
        /// <param name="product">Custom product information.</param>
        /// <param name="cs">Connection string for the MQTT server.</param>
        /// <param name="deviceId">Id of the device.</param>
        /// <param name="telemetryTopicTemplate">Telemetry topic template.</param>
        /// <param name="timeout">Timeout used for operations.</param>
        /// <param name="retry">Retry policy used for operations.</param>
        /// <param name="onConnectionLost">Handler for when the MQTT server connection is lost.</param>
        /// <param name="logger">Logger used for operations</param>
        /// <returns></returns>
        public static async Task<IClient> CreateAsync(
            IManagedMqttClient client,
            string product,
            MqttClientConnectionStringBuilder cs,
            string deviceId,
            string telemetryTopicTemplate,
            TimeSpan timeout,
            IRetryPolicy retry,
            Action onConnectionLost,
            ILogger logger) {
            var options = new MqttClientOptionsBuilder()
                .WithClientId(cs.DeviceId)
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

            var tls_options = new MqttClientOptionsBuilderTlsParameters {
                UseTls = cs.UsingIoTHub,
                SslProtocol = SslProtocols.Tls12,
                IgnoreCertificateRevocationErrors = true
            };

            if (cs.UsingX509Cert) {
                tls_options.Certificates = new List<X509Certificate> { cs.X509Cert };
            }
            options = options.WithTls(tls_options);

            // Use MQTT 5.0 if message expiry interval is set.
            if (cs.MessageExpiryInterval != null) {
                options = options.WithProtocolVersion(MqttProtocolVersion.V500);
            }

            var adapter = new MqttClientAdapter(
                client,
                deviceId,
                timeout,
                onConnectionLost,
                cs.MessageExpiryInterval,
                qualityOfServiceLevel: MqttQualityOfServiceLevel.AtLeastOnce,
                useIoTHubTopics: cs.UsingIoTHub,
                telemetryTopicTemplate: telemetryTopicTemplate,
                logger: logger);

            client.UseConnectedHandler(adapter.OnConnected);
            client.UseDisconnectedHandler(adapter.OnDisconnected);
            client.UseApplicationMessageReceivedHandler(adapter.OnApplicationMessageReceivedHandler);

            var managedOptions = new ManagedMqttClientOptionsBuilder()
                .WithClientOptions(options.Build());

            if (cs.UsingStateFile) {
                managedOptions = managedOptions.WithStorage(new ManagedMqttClientStorage(cs.StateFile, logger));
            }

            await adapter.InternalConnectAsync(managedOptions.Build());
            return adapter;
        }

        /// <summary>
        /// Create and connect an instance of the client adapter.
        /// </summary>
        /// <param name="product">Custom product information.</param>
        /// <param name="cs">Connection string for the MQTT server.</param>
        /// <param name="deviceId">Id of the device.</param>
        /// <param name="telemetryTopicTemplate">Telemetry topic template.</param>
        /// <param name="timeout">Timeout used for operations.</param>
        /// <param name="retry">Retry policy used for operations.</param>
        /// <param name="onConnectionLost">Handler for when the MQTT server connection is lost.</param>
        /// <param name="logger">Logger used for operations</param>
        /// <returns></returns>
        public static Task<IClient> CreateAsync(
            string product,
            MqttClientConnectionStringBuilder cs,
            string deviceId,
            string telemetryTopicTemplate,
            TimeSpan timeout,
            IRetryPolicy retry, Action onConnectionLost, ILogger logger) {
            var client = new MqttFactory().CreateManagedMqttClient();
            return CreateAsync(client, product, cs, deviceId, telemetryTopicTemplate, timeout, retry, onConnectionLost, logger);
        }

        /// <inheritdoc />
        public Task CloseAsync() {
            if (IsClosed) {
                return Task.CompletedTask;
            }
            IsClosed = true;
            return _client.StopAsync();
        }

        /// <inheritdoc />
        public Task SendEventAsync(Message message) {
            if (IsClosed) {
                return Task.CompletedTask;
            }

            // Build merged dictionary of properties to serialize in topic.
            var properties = new Dictionary<string, string>(message.Properties);
            if (!string.IsNullOrWhiteSpace(message.ContentType)) {
                properties[kContentTypePropertyName] = message.ContentType;
            }
            if (!string.IsNullOrWhiteSpace(message.ContentEncoding)) {
                properties[kContentEncodingPropertyName] = message.ContentEncoding;
            }

            string topic;
            if (_useIoTHubTopics || string.IsNullOrWhiteSpace(_telemetryTopicTemplate)) {
                topic = $"devices/{_deviceId}/messages/events/";
                if (properties.Any()) {
                    topic += UrlEncodedDictionarySerializer.Serialize(properties) + kSegmentSeparator;
                    properties.Clear();
                }
            }
            else {
                topic = _telemetryTopicTemplate.Replace(kDeviceIdTemplatePlaceholder, _deviceId);
            }

            return InternalSendEventAsync(topic, message.BodyStream, properties);
        }

        /// <inheritdoc />
        public Task SendEventAsync(string outputName, Message message) {
            throw new InvalidOperationException(
                    "MqttClient does not support specifying output target.");
        }

        /// <inheritdoc />
        public Task SendEventBatchAsync(IEnumerable<Message> messages) {
            if (IsClosed) {
                return Task.CompletedTask;
            }
            return Task.WhenAll(messages.Select(x => SendEventAsync(x)));
        }

        /// <inheritdoc />
        public Task SendEventBatchAsync(string outputName, IEnumerable<Message> messages) {
            if (IsClosed) {
                return Task.CompletedTask;
            }

            throw new InvalidOperationException(
                     "MqttClient does not support specifying output target.");
        }

        /// <inheritdoc />
        public Task SetMethodHandlerAsync(string methodName, MethodCallback methodHandler, object userContext) {
            _methodCallbacks[methodName] = (methodHandler, userContext);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task SetMethodDefaultHandlerAsync(MethodCallback methodHandler, object userContext) {
            _defaultMethodCallback = methodHandler;
            _defaultMethodCallbackUserContext = userContext;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdateCallback callback, object userContext) {
            _desiredPropertyUpdateCallback = callback;
            _desiredPropertyUpdateCallbackUserContext = userContext;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task<Twin> GetTwinAsync() {
            if (IsClosed) {
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
                await InternalSendEventAsync($"$iothub/twin/GET/?$rid={requestId}", cancellationToken: cancellationTokenSource.Token);
                while (!cancellationTokenSource.Token.IsCancellationRequested) {
                    _responseHandle.WaitOne(_timeout);
                    _responseHandle.Reset();
                    if (_responses[requestId] != null) {
                        result = new Twin {
                            Properties = JsonConvert.DeserializeObject<TwinProperties>(Encoding.UTF8.GetString(_responses[requestId].Payload)),
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

            using var payload = new MemoryStream(Encoding.UTF8.GetBytes(reportedProperties.ToJson()));
            await InternalSendEventAsync("$iothub/twin/PATCH/properties/reported/?$rid=patch_temp", payload);
        }

        /// <inheritdoc />
        public Task UploadToBlobAsync(string blobName, Stream source) {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public Task<MethodResponse> InvokeMethodAsync(string deviceId, string moduleId, MethodRequest methodRequest, CancellationToken cancellationToken) {
            throw new NotSupportedException("MQTT client does not support methods");
        }

        /// <inheritdoc />
        public Task<MethodResponse> InvokeMethodAsync(string deviceId, MethodRequest methodRequest, CancellationToken cancellationToken) {
            throw new NotSupportedException("MQTT client does not support methods");
        }

        /// <inheritdoc />
        public void Dispose() {
            IsClosed = true;
            _client?.Dispose();
        }

        /// <summary>
        /// Connect the MQTT client.
        /// </summary>
        /// <param name="options">Options for the MQTT client connection</param>
        /// <returns>Whether the conneciton was successful.</returns>
        private async Task<bool> InternalConnectAsync(IManagedMqttClientOptions options) {
            try {
                if (!_client.IsStarted) {
                    // Start the client which connects to the server in the background. Wait for the client to
                    // signal that it is connected before.
                    await _client.StartAsync(options);
                    _connectedHandle.WaitOne(_timeout);

                    // The managed client is too slow when subscribing to the topics and can miss immediate
                    // messages. Call the internal client and subscribe directly to solve the issue. According
                    // to the MQTT specification, the approach is compliant. It is not possible to end up with
                    // multiple subscriptions with identical topic filters. While the server replaces a
                    // subscription, the flow of publications will not be interrupted. See specification:
                    // http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/os/mqtt-v3.1.1-os.html#_Toc398718067
                    var topicFilters = new MqttTopicFilter[] {
                            new MqttTopicFilter { Topic = "$iothub/twin/res/#" },
                            new MqttTopicFilter { Topic = "$iothub/twin/PATCH/properties/desired/#" },
                            new MqttTopicFilter { Topic = "$iothub/methods/POST/#" }
                        };
                    await _client.InternalClient.SubscribeAsync(topicFilters);
                    await _client.SubscribeAsync(topicFilters);
                }
                return true;
            }
            catch (TaskCanceledException) {
                _logger.Error("Failed to connect to MQTT broker due to timeout: {Timeout}", _timeout);
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to connect to MQTT broker.");
            }
            return false;
        }

        /// <summary>
        /// Send event as MQTT message with configured properties for the client.
        /// </summary>
        /// <param name="topic">Topic for MQTT message.</param>
        /// <param name="payload">Optional payload for MQTT message.</param>
        /// <param name="userProperties">Optional user properties for MQTT message.</param>
        /// <param name="cancellationToken">Optional cancellation token for operation.</param>
        /// <returns></returns>
        private Task InternalSendEventAsync(
            string topic,
            Stream payload = null,
            IDictionary<string, string> userProperties = null,
            CancellationToken cancellationToken = default) {

            _logger.Debug("Publishing {ByteCount} bytes to {Topic}", payload != null ? payload.Length : 0, topic);
            // Check topic length.
            var topicLength = Encoding.UTF8.GetByteCount(topic);
            if (topicLength > kMaxTopicLength) {
                throw new MessageTooLargeException(
                    $"Topic for MQTT message cannot be larger than {kMaxTopicLength} bytes, but current length is {topicLength}. " +
                    $"The list of message.properties and/or message.systemProperties is likely too long. Please use AMQP or HTTP.");
            }

            CancellationTokenSource cancellationTokenSource = null;

            // Create default cancellation token.
            if (cancellationToken == default) {
                cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.CancelAfter(_timeout);
                cancellationToken = cancellationTokenSource.Token;
            }

            try {
                // Build MQTT message.
                var mqttApplicationMessageBuilder = new MqttApplicationMessageBuilder()
                    .WithQualityOfServiceLevel(_qualityOfServiceLevel)
                    .WithContentType(kContentType)
                    .WithTopic(topic)
                    .WithRetainFlag(true);

                if (payload != null) {
                    mqttApplicationMessageBuilder = mqttApplicationMessageBuilder.WithPayload(payload);
                }
                if (_messageExpiryInterval != null) {
                    mqttApplicationMessageBuilder = mqttApplicationMessageBuilder.WithMessageExpiryInterval(_messageExpiryInterval.Value);
                }
                if (userProperties != null) {
                    foreach (var userProperty in userProperties) {
                        mqttApplicationMessageBuilder.WithUserProperty(userProperty.Key, userProperty.Value);
                    }
                }

                return _client.PublishAsync(mqttApplicationMessageBuilder.Build(), cancellationToken);
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to publish MQTT message.");
            }
            finally {
                cancellationTokenSource?.Dispose();
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Handler for when the MQTT client is connected.
        /// </summary>
        /// <param name="eventArgs">Event arguments.</param>
        /// <returns></returns>
        private void OnConnected(MqttClientConnectedEventArgs eventArgs) {
            _connectedHandle.Set();
            _logger.Information("{counter}: Device {deviceId} connected or reconnected", _reconnectCounter, _deviceId);
            kReconnectionStatus.WithLabels(_deviceId).Set(_reconnectCounter);
            _reconnectCounter++;
        }

        /// <summary>
        /// Handler for when the MQTT client is disconnected.
        /// </summary>
        /// <param name="eventArgs">Event arguments.</param>
        /// <returns></returns>
        private async void OnDisconnected(MqttClientDisconnectedEventArgs eventArgs) {
            _connectedHandle.Reset();
            _logger.Information("{counter}: Device {deviceId} disconnected due to {reason}", _reconnectCounter, _deviceId, eventArgs.Reason);
            kDisconnectionStatus.WithLabels(_deviceId).Set(_reconnectCounter);
            if (IsClosed) {
                return;
            }

            // Try to reconnect.
            await Task.Delay(3000);
            if (!await InternalConnectAsync(_client.Options)) {
                _onConnectionLost();
            }
        }

        /// <summary>
        /// Handler for when a response is received.
        /// </summary>
        /// <param name="eventArgs">Event arguments.</param>
        /// <returns></returns>
        private void OnResponseReceived(MqttApplicationMessageReceivedEventArgs eventArgs) {
            // Only store the response if a thread is waiting for it (indicated by key added).
            var requestId = eventArgs.ApplicationMessage.Topic.Substring("$iothub/twin/res/200/?$rid=".Length);
            if (_responses.ContainsKey(requestId)) {
                _responses[requestId] = eventArgs.ApplicationMessage;
            }

            // Unblock all threads waiting for responses.
            _responseHandle.Set();
        }

        /// <summary>
        /// Handler for when desired properties are updated.
        /// </summary>
        /// <param name="eventArgs">Event arguments.</param>
        /// <returns></returns>
        private void OnDesiredPropertiesUpdated(MqttApplicationMessageReceivedEventArgs eventArgs) {
            var twinCollection = JsonConvert.DeserializeObject<TwinCollection>(Encoding.UTF8.GetString(eventArgs.ApplicationMessage.Payload));
            _desiredPropertyUpdateCallback?.Invoke(twinCollection, _desiredPropertyUpdateCallbackUserContext);
        }

        /// <summary>
        /// Handler for when a method is called.
        /// </summary>
        /// <param name="eventArgs">Event arguments.</param>
        /// <returns></returns>
        private async void OnMethodCalled(MqttApplicationMessageReceivedEventArgs eventArgs) {
            // Parse topic.
            var components = eventArgs.ApplicationMessage.Topic.Split('/');
            var methodName = components[components.Length - 2];
            var requestId = components[components.Length - 1].Substring("?$rid=".Length);

            // Get callback and user context.
            var callback = _defaultMethodCallback;
            if (_methodCallbacks.ContainsKey(methodName)) {
                callback = _methodCallbacks[methodName].methodCallback;
            }

            if (callback == null) {
                return;
            }

            // Invoke callback.
            var methodRequest = new MethodRequest(methodName, eventArgs.ApplicationMessage.Payload);
            try {
                var methodResponse = await callback.Invoke(methodRequest, _defaultMethodCallbackUserContext);
                MemoryStream payload = null;
                if (methodResponse.Result != null && methodResponse.Result.Any()) {
                    payload = new MemoryStream(methodResponse.Result);
                }
                await InternalSendEventAsync($"$iothub/methods/res/{methodResponse.Status}/?$rid={requestId}", payload);
                payload?.Dispose();
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to call method \"{MethodName}\"", methodName);
                await InternalSendEventAsync($"$iothub/methods/res/{(int)HttpStatusCode.InternalServerError}/?$rid={requestId}");
            }
        }

        /// <summary>
        /// Handler for when the MQTT client receives an application message.
        /// </summary>
        /// <param name="eventArgs">Event arguments.</param>
        /// <returns></returns>
        private void OnApplicationMessageReceivedHandler(MqttApplicationMessageReceivedEventArgs eventArgs) {
            if (eventArgs.ProcessingFailed) {
                _logger.Warning("Failed to process MQTT message: {reasonCode}", eventArgs.ReasonCode);
                return;
            }

            try {
                if (eventArgs.ApplicationMessage.Topic.StartsWith("$iothub/twin/res/200/?$rid=", StringComparison.OrdinalIgnoreCase)) {
                    OnResponseReceived(eventArgs);
                }
                else if (eventArgs.ApplicationMessage.Topic.StartsWith("$iothub/twin/PATCH/properties/desired/?$version=", StringComparison.OrdinalIgnoreCase)) {
                    OnDesiredPropertiesUpdated(eventArgs);
                }
                else if (eventArgs.ApplicationMessage.Topic.StartsWith("$iothub/methods/POST/", StringComparison.OrdinalIgnoreCase)) {
                    OnMethodCalled(eventArgs);
                }
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to process MQTT message.");
            }
        }

        private readonly IManagedMqttClient _client;
        private readonly MqttQualityOfServiceLevel _qualityOfServiceLevel;
        private readonly bool _useIoTHubTopics;
        private readonly string _telemetryTopicTemplate;

        private const int kMaxTopicLength = 0xffff;
        private const string kSegmentSeparator = "/";
        private const string kContentEncodingPropertyName = "iothub-content-encoding";
        private const string kContentTypePropertyName = "iothub-content-type";
        private const string kContentType = "application/json";
        private const string kDeviceIdTemplatePlaceholder = "{device_id}";

        private readonly string _deviceId;
        private readonly TimeSpan _timeout;
        private readonly Action _onConnectionLost;
        private readonly ILogger _logger;
        private readonly uint? _messageExpiryInterval;
        private ManualResetEvent _responseHandle = new ManualResetEvent(false);
        private ManualResetEvent _connectedHandle = new ManualResetEvent(false);
        private ConcurrentDictionary<string, MqttApplicationMessage> _responses = new ConcurrentDictionary<string, MqttApplicationMessage>();
        private DesiredPropertyUpdateCallback _desiredPropertyUpdateCallback;
        private object _desiredPropertyUpdateCallbackUserContext;
        private MethodCallback _defaultMethodCallback;
        private object _defaultMethodCallbackUserContext;
        private readonly Dictionary<string, (MethodCallback methodCallback, object userContext)> _methodCallbacks = new Dictionary<string, (MethodCallback methodCallback, object methodCallbackUserContext)>();

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
