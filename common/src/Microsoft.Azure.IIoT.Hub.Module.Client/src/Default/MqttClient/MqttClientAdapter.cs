// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client.MqttClient
{
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Common;
    using Microsoft.Azure.Devices.Shared;
    using Microsoft.Extensions.Logging;
    using Furly.Exceptions;
    using MQTTnet;
    using MQTTnet.Client;
    using MQTTnet.Extensions.ManagedClient;
    using MQTTnet.Formatter;
    using MQTTnet.Packets;
    using MQTTnet.Protocol;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.Metrics;
    using System.Globalization;
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
    public sealed class MqttClientAdapter : IClient
    {
        /// <inheritdoc />
        public int MaxEventPayloadSizeInBytes => int.MaxValue;

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
            : this(metrics ?? throw new ArgumentNullException(nameof(metrics)))
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _deviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
            _timeout = timeout;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _protocolVersion = protocolVersion;
            _qualityOfServiceLevel = qualityOfServiceLevel;
            _methodTopicRoot = "$iothub/methods";
            _twinTopicRoot = "$iothub/twin";

            if (!string.IsNullOrWhiteSpace(telemetryTopicTemplate))
            {
                if (!telemetryTopicTemplate.Contains(kOutputNamePlaceholder, StringComparison.Ordinal))
                {
                    if (telemetryTopicTemplate.EndsWith('/'))
                    {
                        _telemetryTopicTemplate =
                            $"{telemetryTopicTemplate}{kOutputNamePlaceholder}/";
                    }
                    else
                    {
                        _telemetryTopicTemplate =
                            $"{telemetryTopicTemplate}/{kOutputNamePlaceholder}";
                    }
                }
                else
                {
                    _telemetryTopicTemplate = telemetryTopicTemplate;
                }
                _methodTopicRoot = _telemetryTopicTemplate
                    .Replace(kDeviceIdTemplatePlaceholder, _deviceId, StringComparison.Ordinal)
                    .Replace(kOutputNamePlaceholder, "methods", StringComparison.Ordinal)
                    .Replace("//", "/", StringComparison.Ordinal)
                    ;
                _twinTopicRoot = _telemetryTopicTemplate
                    .Replace(kDeviceIdTemplatePlaceholder, _deviceId, StringComparison.Ordinal)
                    .Replace(kOutputNamePlaceholder, "twin", StringComparison.Ordinal)
                    .Replace("//", "/", StringComparison.Ordinal)
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
            TimeSpan timeout, ILogger logger, IMetricsContext metrics)
        {
            var options = new MqttClientOptionsBuilder()
                .WithClientId(string.IsNullOrEmpty(cs.DeviceId) ? Guid.NewGuid().ToString() : cs.DeviceId)
                .WithTcpServer(tcpOptions =>
                {
                    tcpOptions.Server = cs.HostName;
                    tcpOptions.Port = cs.Port;

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        tcpOptions.BufferSize = 64 * 1024;
                    }
                });

            if (!string.IsNullOrWhiteSpace(cs.Username) && !string.IsNullOrWhiteSpace(cs.Password))
            {
                options = options.WithCredentials(cs.Username, cs.Password);
            }

            var tlsOptions = new MqttClientOptionsBuilderTlsParameters
            {
                UseTls = cs.UsingIoTHub,
                SslProtocol = SslProtocols.Tls12,
                IgnoreCertificateRevocationErrors = true
            };

            if (cs.UsingX509Cert)
            {
                tlsOptions.Certificates = new List<X509Certificate> { cs.X509Cert };
            }
            options = options.WithTls(tlsOptions);

            if (cs.Protocol != MqttProtocolVersion.Unknown)
            {
                options = options.WithProtocolVersion(cs.Protocol);
            }

            var adapter = new MqttClientAdapter(client, deviceId, timeout, cs.Protocol,
                qualityOfServiceLevel: MqttQualityOfServiceLevel.AtLeastOnce,
                telemetryTopicTemplate: cs.UsingIoTHub ? null : telemetryTopicTemplate,
                logger: logger, metrics: metrics);

            client.ConnectedAsync += adapter.OnConnectedAsync;
            client.ConnectingFailedAsync += adapter.OnConnectingFailedAsync;
            client.ConnectionStateChangedAsync += adapter.OnConnectionStateChangedAsync;
            client.SynchronizingSubscriptionsFailedAsync += adapter.SynchronizingSubscriptionsFailedAsync;
            client.DisconnectedAsync += adapter.OnDisconnectedAsync;
            client.ApplicationMessageReceivedAsync += adapter.OnApplicationMessageReceivedAsync;

            var managedOptions = new ManagedMqttClientOptionsBuilder()
                .WithClientOptions(options.Build())
                .WithAutoReconnectDelay(TimeSpan.FromMilliseconds(300))
                .WithPendingMessagesOverflowStrategy(
                    MQTTnet.Server.MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage)
                ;
            if (cs.UsingStateFile)
            {
                managedOptions = managedOptions.WithStorage(new ManagedMqttClientStorage(cs.StateFile, logger));
            }

            await adapter.StartAsync(managedOptions.Build()).ConfigureAwait(false);
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
            IMetricsContext metrics)
        {
            var client = new MqttFactory().CreateManagedMqttClient();
            return CreateAsync(client, cs, deviceId, telemetryTopicTemplate, timeout, logger,
                metrics);
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (IsClosed)
            {
                return;
            }
            IsClosed = true;
            await _client.StopAsync().ConfigureAwait(false);
        }

        /// <inheritdoc />
        public IEvent CreateEvent()
        {
            return new MqttClientAdapterMessage(this);
        }

        /// <inheritdoc />
        public Task SetMethodHandlerAsync(MethodCallback methodHandler)
        {
            _defaultMethodCallback = methodHandler;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdateCallback callback)
        {
            _desiredPropertyUpdateCallback = callback;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task<Twin> GetTwinAsync()
        {
            if (IsClosed || _telemetryTopicTemplate != null)
            {
                return null;
            }

            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(_timeout);

            // Signal that the response should be saved by setting the key.
            var requestId = Guid.NewGuid().ToString();
            _responses[requestId] = null;

            Twin result = null;
            try
            {
                // Publish message and wait for response to come back. The thread may be unblocked by other
                // simultaneous calls, so wait again if needed.
                await InternalSendEventAsync($"{_twinTopicRoot}/GET/?$rid={requestId}").ConfigureAwait(false);
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    _responseHandle.WaitOne(_timeout);
                    _responseHandle.Reset();
                    if (_responses[requestId] != null)
                    {
                        result = new Twin
                        {
                            Properties = JsonConvert.DeserializeObject<TwinProperties>(
                                Encoding.UTF8.GetString(_responses[requestId].Payload))
                        };
                        break;
                    }
                }

                if (result == null)
                {
                    cancellationTokenSource.Token.ThrowIfCancellationRequested();
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogError("Failed to get twin due to timeout: {Timeout}", _timeout);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get twin.");
            }
            _responses.Remove(requestId, out _);
            return result;
        }

        /// <inheritdoc />
        public async Task UpdateReportedPropertiesAsync(TwinCollection reportedProperties)
        {
            if (IsClosed)
            {
                return;
            }
            var payload = Encoding.UTF8.GetBytes(reportedProperties.ToJson());
            await InternalSendEventAsync(
                $"{_twinTopicRoot}/PATCH/properties/reported/?$rid=patch_temp", payload).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task<MethodResponse> InvokeMethodAsync(string deviceId, string moduleId,
            MethodRequest methodRequest, CancellationToken cancellationToken)
        {
            throw new NotSupportedException("MQTT client does not support methods");
        }

        /// <inheritdoc />
        public void Dispose()
        {
            IsClosed = true;
            _client?.Dispose();
        }

        /// <summary>
        /// Send event as MQTT message with configured properties for the client.
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="payload"></param>
        /// <param name="contentType"></param>
        /// <param name="retain"></param>
        /// <param name="ttl"></param>
        /// <param name="properties"></param>
        /// <param name="correlationData"></param>
        /// <returns></returns>
        /// <exception cref="MessageSizeLimitException"></exception>
        private async Task InternalSendEventAsync(string topic, ReadOnlyMemory<byte> payload = default,
            string contentType = null, bool retain = false, TimeSpan? ttl = null,
            Dictionary<string, string> properties = null, byte[] correlationData = null)
        {
            _logger.LogDebug("Publishing {ByteCount} bytes to {Topic}", payload.Length, topic);

            // Check topic length.
            var topicLength = Encoding.UTF8.GetByteCount(topic);
            if (topicLength > kMaxTopicLength)
            {
                throw new MessageSizeLimitException(
                    $"Topic for MQTT message cannot be larger than {kMaxTopicLength} bytes, " +
                    $"but current length is {topicLength}. The list of message.properties " +
                    "and/or message.systemProperties is likely too long. Please use AMQP or HTTP.");
            }
            try
            {
                // Build MQTT message.
                var mqttApplicationMessageBuilder = new MqttApplicationMessageBuilder()
                    .WithQualityOfServiceLevel(_qualityOfServiceLevel)
                    .WithContentType(contentType ?? kContentType)
                    .WithTopic(topic)
                    .WithRetainFlag(retain)
                    ;

                if (correlationData != null)
                {
                    mqttApplicationMessageBuilder.WithCorrelationData(correlationData);
                }
                if (payload.Length > 0)
                {
                    mqttApplicationMessageBuilder = mqttApplicationMessageBuilder.WithPayload(payload.ToArray());
                }
                if (ttl > TimeSpan.Zero)
                {
                    mqttApplicationMessageBuilder = mqttApplicationMessageBuilder
                        .WithMessageExpiryInterval((uint)ttl.Value.TotalSeconds);
                }
                if (properties != null)
                {
                    foreach (var userProperty in properties)
                    {
                        mqttApplicationMessageBuilder.WithUserProperty(userProperty.Key, userProperty.Value);
                    }
                }
                var mqttmessage = mqttApplicationMessageBuilder.Build();
                await _client.EnqueueAsync(mqttmessage).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish MQTT message.");
            }
        }

        /// <summary>
        /// Handler for when the internal MQTT client is connected.
        /// </summary>
        /// <param name="eventArgs">Event arguments.</param>
        /// <returns></returns>
        private Task OnConnectedAsync(MqttClientConnectedEventArgs eventArgs)
        {
            _logger.LogInformation("{Counter}: Device {DeviceId} connected or reconnected",
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
        private Task SynchronizingSubscriptionsFailedAsync(ManagedProcessFailedEventArgs eventArgs)
        {
            _logger.LogInformation("{Counter}: Device {DeviceId} subscription sync failed.",
                _reconnectCounter, _deviceId);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handler for when the connection state failed
        /// </summary>
        /// <param name="eventArgs">Event arguments.</param>
        /// <returns></returns>
        private Task OnConnectionStateChangedAsync(EventArgs eventArgs)
        {
            _logger.LogInformation("{Counter}: Device {DeviceId} connection state changed.",
                _reconnectCounter, _deviceId);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handler for when connecting failed
        /// </summary>
        /// <param name="eventArgs">Event arguments.</param>
        /// <returns></returns>
        private Task OnConnectingFailedAsync(ConnectingFailedEventArgs eventArgs)
        {
            IsConnected = false;

            _logger.LogInformation("{Counter}: Device {DeviceId} disconnected due to {Reason}",
                 _reconnectCounter, _deviceId, eventArgs.ToString());

            // _onConnectionLost?.Invoke();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handler for when the internal MQTT client is disconnected.
        /// </summary>
        /// <param name="eventArgs">Event arguments.</param>
        /// <returns></returns>
        private Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs eventArgs)
        {
            if (!IsConnected)
            {
                return Task.CompletedTask;
            }
            IsConnected = false;

            _logger.LogInformation("{Counter}: Device {DeviceId} disconnected due to {Reason}",
                 _reconnectCounter, _deviceId, eventArgs.Reason);

            // _onConnectionLost?.Invoke();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Subscribe to all required topics
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private async Task StartAsync(ManagedMqttClientOptions options)
        {
            // Start the client which connects to the server in the background.
            await _client.StartAsync(options).ConfigureAwait(false);

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
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Handler for when a response is received.
        /// </summary>
        /// <param name="eventArgs">Event arguments.</param>
        /// <returns></returns>
        private Task OnTwinResponseAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            // Only handling twin GET response for now.

            // Only store the response if a thread is waiting for it (indicated by key added).
            var requestId = eventArgs.ApplicationMessage.Topic.Substring(
                $"{_twinTopicRoot}/res/200/?$rid=".Length);
            if (_responses.ContainsKey(requestId))
            {
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
        private Task OnDesiredPropertiesUpdatedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
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
        private async Task OnMethodRequestAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            // Parse topic.
            var components = eventArgs.ApplicationMessage.Topic.Split('/');

            string methodName;
            string requestId = null;
            if (IsMqttv5)
            {
                if (string.IsNullOrEmpty(eventArgs.ApplicationMessage.ResponseTopic))
                {
                    // No response topic
                    return;
                }
                methodName = components[components.Length - 1];
            }
            else
            {
                if (components.Length < 2)
                {
                    return;
                }
                methodName = components[components.Length - 2];
                requestId = components[components.Length - 1].Substring("?$rid=".Length);
            }

            // Get callback and user context.
            var callback = _defaultMethodCallback;
            if (callback == null)
            {
                return;
            }

            // Invoke callback.
            var methodRequest = new MethodRequest(methodName, eventArgs.ApplicationMessage.Payload);
            try
            {
                var methodResponse = await callback.Invoke(methodRequest, null).ConfigureAwait(false);
                var payload = methodResponse.Result?.Length > 0 ? methodResponse.Result : null;
                var statusCode = methodResponse.Status.ToString(CultureInfo.InvariantCulture);
                if (IsMqttv5)
                {
                    await InternalSendEventAsync(eventArgs.ApplicationMessage.ResponseTopic, payload,
                        properties: new Dictionary<string, string> { [kStatusCodeKey] = statusCode },
                        correlationData: eventArgs.ApplicationMessage.CorrelationData).ConfigureAwait(false);
                }
                else
                {
                    await InternalSendEventAsync(
                        $"{_methodTopicRoot}/res/{statusCode}/?$rid={requestId}", payload).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to call method \"{MethodName}\"", methodName);
                var statusCode = ((int)HttpStatusCode.InternalServerError).ToString(CultureInfo.InvariantCulture);
                if (IsMqttv5)
                {
                    await InternalSendEventAsync(eventArgs.ApplicationMessage.ResponseTopic,
                        properties: new Dictionary<string, string> { [kStatusCodeKey] = statusCode },
                        correlationData: eventArgs.ApplicationMessage.CorrelationData).ConfigureAwait(false);
                }
                else
                {
                    await InternalSendEventAsync(
                        $"{_methodTopicRoot}/res/{statusCode}/?$rid={requestId}").ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Handler for when a method response was received
        /// </summary>
        /// <param name="eventArgs"></param>
        private static Task OnMethodResponseAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            // TODO: Support responses
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handler for when the MQTT client receives an application message.
        /// </summary>
        /// <param name="eventArgs">Event arguments.</param>
        /// <returns></returns>
        private async Task OnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            if (eventArgs.ProcessingFailed)
            {
                _logger.LogWarning("Failed to process MQTT message: {ReasonCode}", eventArgs.ReasonCode);
                return;
            }
            var topic = eventArgs.ApplicationMessage.Topic;
            try
            {
                if (topic.StartsWith($"{_methodTopicRoot}/res/", StringComparison.OrdinalIgnoreCase))
                {
                    //
                    // Handle method responses for methods called on other components.
                    // All responses are diverted to topics starting with /res either through
                    // the 3.11 method of using $rid= or through response topics.
                    //
                    await OnMethodResponseAsync(eventArgs).ConfigureAwait(false);
                }
                else if (topic.StartsWith($"{_methodTopicRoot}/", StringComparison.OrdinalIgnoreCase))
                {
                    //
                    // typically should start with method topic root and /POST/ element but
                    // we allow any topic path components in between it and the method.
                    //
                    await OnMethodRequestAsync(eventArgs).ConfigureAwait(false);
                }
                else if (topic.StartsWith($"{_twinTopicRoot}/res/200/?$rid=",
                    StringComparison.OrdinalIgnoreCase))
                {
                    await OnTwinResponseAsync(eventArgs).ConfigureAwait(false);
                }
                else if (topic.StartsWith($"{_twinTopicRoot}/PATCH/properties/desired/?$version=",
                    StringComparison.OrdinalIgnoreCase))
                {
                    await OnDesiredPropertiesUpdatedAsync(eventArgs).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process MQTT message.");
            }
        }

        /// <summary>
        /// Message wrapper
        /// </summary>
        internal sealed class MqttClientAdapterMessage : IEvent
        {
            /// <inheritdoc/>
            public IEvent SetContentType(string value)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _userProperties[kContentTypePropertyName] = value;
                    if (_outer._telemetryTopicTemplate == null)
                    {
                        _userProperties[SystemProperties.MessageSchema] = value;
                    }
                }
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetContentEncoding(string value)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _userProperties[kContentEncodingPropertyName] = value;
                    if (_outer._telemetryTopicTemplate == null)
                    {
                        _userProperties[CommonProperties.ContentEncoding] = value;
                    }
                }
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetMessageSchema(string value)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _userProperties[CommonProperties.EventSchemaType] = value;
                }
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetRoutingInfo(string value)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _userProperties[CommonProperties.RoutingInfo] = value;
                }
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetTopic(string value)
            {
                _topic = value;
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetRetain(bool value)
            {
                _retain = value;
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetTtl(TimeSpan value)
            {
                _ttl = value;
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetTimestamp(DateTime value)
            {
                return this;
            }

            /// <inheritdoc/>
            public IEvent AddBuffers(IReadOnlyList<ReadOnlyMemory<byte>> value)
            {
                _buffers.AddRange(value);
                return this;
            }

            /// <summary>
            /// User properties
            /// </summary>
            internal Dictionary<string, string> UserProperties
                => _outer._telemetryTopicTemplate == null ? null : _userProperties;

            /// <summary>
            /// Topic
            /// </summary>
            internal string BuildTopicPath()
            {
                // Build merged dictionary of properties to serialize in topic.
                if (_outer._telemetryTopicTemplate == null)
                {
                    var topic = $"devices/{_outer._deviceId}/messages/events/";
                    if (_userProperties.Count > 0)
                    {
                        topic += UrlEncodedDictionarySerializer.Serialize(_userProperties) + kSegmentSeparator;
                    }
                    return topic;
                }
                else
                {
                    return _outer._telemetryTopicTemplate
                        .Replace(kDeviceIdTemplatePlaceholder, _outer._deviceId, StringComparison.Ordinal)
                        .Replace(kOutputNamePlaceholder, _topic ?? string.Empty, StringComparison.Ordinal)
                        .Replace("//", "/", StringComparison.Ordinal)
                        ;
                }
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                _userProperties.Clear();
                _buffers.Clear();
            }

            /// <inheritdoc />
            public async Task SendAsync(CancellationToken ct)
            {
                if (_outer.IsClosed)
                {
                    return;
                }
                var topic = BuildTopicPath();
                var contentType = _userProperties[kContentTypePropertyName];
                foreach (var body in _buffers)
                {
                    if (!body.IsEmpty)
                    {
                        await _outer.InternalSendEventAsync(topic, body, contentType, _retain,
                            _ttl, UserProperties).ConfigureAwait(false);
                    }
                }
            }

            /// <summary>
            /// Create message
            /// </summary>
            /// <param name="outer"></param>
            internal MqttClientAdapterMessage(MqttClientAdapter outer)
            {
                _outer = outer;
            }

            private readonly MqttClientAdapter _outer;
            private string _topic;
            private bool _retain;
            private TimeSpan _ttl;
            private readonly List<ReadOnlyMemory<byte>> _buffers = new();
            private readonly Dictionary<string, string> _userProperties = new();
        }

        /// <summary>
        /// Create observable metrics
        /// </summary>
        /// <param name="metrics"></param>
        private MqttClientAdapter(IMetricsContext metrics)
        {
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
        private readonly ManualResetEvent _responseHandle = new(false);
        private readonly ConcurrentDictionary<string, MqttApplicationMessage> _responses = new();
        private DesiredPropertyUpdateCallback _desiredPropertyUpdateCallback;
        private MethodCallback _defaultMethodCallback;
        private int _reconnectCounter;
    }
}
