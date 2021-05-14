// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client {
    using Microsoft.Azure.IIoT.Abstractions;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Microsoft.Azure.Devices.Shared;
    using Serilog;
    using Serilog.Events;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using System.Threading;
    using System.Diagnostics.Tracing;
    using Prometheus;

    /// <summary>
    /// Injectable factory that creates clients from device sdk
    /// </summary>
    public sealed class IoTSdkFactory : IClientFactory, IDisposable {

        /// <inheritdoc />
        public string DeviceId { get; }

        /// <inheritdoc />
        public string ModuleId { get; }

        /// <inheritdoc />
        public string Gateway { get; }

        /// <inheritdoc />
        public IRetryPolicy RetryPolicy { get; set; }

        /// <summary>
        /// Create sdk factory
        /// </summary>
        /// <param name="config"></param>
        /// <param name="broker"></param>
        /// <param name="logger"></param>
        public IoTSdkFactory(IModuleConfig config, IEventSourceBroker broker, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (broker != null) {
                _logHook = broker.Subscribe(IoTSdkLogger.EventSource, new IoTSdkLogger(logger));
            }

            // The runtime injects this as an environment variable
            var deviceId = Environment.GetEnvironmentVariable(IoTEdgeVariables.IOTEDGE_DEVICEID);
            var moduleId = Environment.GetEnvironmentVariable(IoTEdgeVariables.IOTEDGE_MODULEID);
            var ehubHost = Environment.GetEnvironmentVariable(IoTEdgeVariables.IOTEDGE_GATEWAYHOSTNAME);

            try {
                if (!string.IsNullOrEmpty(config.EdgeHubConnectionString)) {
                    _cs = IotHubConnectionStringBuilder.Create(config.EdgeHubConnectionString);

                    if (string.IsNullOrEmpty(_cs.SharedAccessKey)) {
                        throw new InvalidConfigurationException(
                            "Connection string is missing shared access key.");
                    }
                    if (string.IsNullOrEmpty(_cs.DeviceId)) {
                        throw new InvalidConfigurationException(
                            "Connection string is missing device id.");
                    }

                    deviceId = _cs.DeviceId;
                    moduleId = _cs.ModuleId;
                    ehubHost = _cs.GatewayHostName ?? ehubHost;

                    if (string.IsNullOrWhiteSpace(_cs.GatewayHostName) && !string.IsNullOrWhiteSpace(ehubHost)) {
                        _cs = IotHubConnectionStringBuilder.Create(
                            config.EdgeHubConnectionString + ";GatewayHostName=" + ehubHost);

                        _logger.Information($"Details of gateway host are added to IoT Hub connection string: " +
                            $"GatewayHostName={ehubHost}");
                    }

                }
            }
            catch (Exception e) {
                _logger.Error(e, "Bad configuration value in EdgeHubConnectionString config.");
            }

            ModuleId = moduleId;
            DeviceId = deviceId;
            Gateway = ehubHost;

            if (string.IsNullOrEmpty(DeviceId)) {
                var ex = new InvalidConfigurationException(
                    "If you are running outside of an IoT Edge context or in EdgeHubDev mode, then the " +
                    "host configuration is incomplete and missing the EdgeHubConnectionString setting." +
                    "You can run the module using the command line interface or in IoT Edge context, or " +
                    "manually set the 'EdgeHubConnectionString' environment variable.");

                _logger.Error(ex, "The Twin module was not configured correctly.");
                throw ex;
            }

            _bypassCertValidation = config.BypassCertVerification;
            if (!_bypassCertValidation) {
                var certPath = Environment.GetEnvironmentVariable("EdgeModuleCACertificateFile");
                if (!string.IsNullOrWhiteSpace(certPath)) {
                    InstallCert(certPath);
                }
                else if (!string.IsNullOrEmpty(ehubHost)) {
                    _bypassCertValidation = true;
                }
            }
            if (!string.IsNullOrEmpty(ehubHost)) {
                // Running in edge mode
                // the configured transport (if provided) will be forced to it's OverTcp
                // variant as follows: AmqpOverTcp when Amqp, AmqpOverWebsocket or AmqpOverTcp specified
                // and MqttOverTcp otherwise. Default is MqttOverTcp
                if ((config.Transport & TransportOption.Mqtt) != 0) {
                    // prefer Mqtt over Amqp due to performance reasons
                    _transport = TransportOption.MqttOverTcp;
                }
                else {
                    _transport = TransportOption.AmqpOverTcp;
                }
                _logger.Information("Connecting all clients to {edgeHub} using {transport}.",
                    ehubHost, _transport);
            }
            else {
                _transport = config.Transport;
            }
            _timeout = TimeSpan.FromMinutes(5);
        }

        /// <inheritdoc/>
        public void Dispose() {
            _logHook?.Dispose();
        }


        /// <inheritdoc/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5359:Do Not Disable Certificate Validation", 
            Justification = "<Pending>")]
        public async Task<IClient> CreateAsync(string product, IProcessControl ctrl) {

            if (_bypassCertValidation) {
                _logger.Warning("Bypassing certificate validation for client.");
            }

            // Configure transport settings
            var transportSettings = new List<ITransportSettings>();

            if ((_transport & TransportOption.MqttOverTcp) != 0) {
                var setting = new MqttTransportSettings(
                    TransportType.Mqtt_Tcp_Only);
                if (_bypassCertValidation) {
                    setting.RemoteCertificateValidationCallback =
                        (sender, certificate, chain, sslPolicyErrors) => true;
                }
                transportSettings.Add(setting);
            }
            if ((_transport & TransportOption.MqttOverWebsocket) != 0) {
                var setting = new MqttTransportSettings(
                    TransportType.Mqtt_WebSocket_Only);
                if (_bypassCertValidation) {
                    setting.RemoteCertificateValidationCallback =
                        (sender, certificate, chain, sslPolicyErrors) => true;
                }
                transportSettings.Add(setting);
            }
            if ((_transport & TransportOption.AmqpOverTcp) != 0) {
                var setting = new AmqpTransportSettings(
                    TransportType.Amqp_Tcp_Only);
                if (_bypassCertValidation) {
                    setting.RemoteCertificateValidationCallback =
                        (sender, certificate, chain, sslPolicyErrors) => true;
                }
                transportSettings.Add(setting);
            }
            if ((_transport & TransportOption.AmqpOverWebsocket) != 0) {
                var setting = new AmqpTransportSettings(
                    TransportType.Amqp_WebSocket_Only);
                if (_bypassCertValidation) {
                    setting.RemoteCertificateValidationCallback =
                        (sender, certificate, chain, sslPolicyErrors) => true;
                }
                transportSettings.Add(setting);
            }
            if (transportSettings.Count != 0) {
                return await Try.Options(transportSettings
                    .Select<ITransportSettings, Func<Task<IClient>>>(t =>
                         () => CreateAdapterAsync(product, () => ctrl?.Reset(), t))
                    .ToArray());
            }
            return await CreateAdapterAsync(product, () => ctrl?.Reset());
        }

        /// <summary>
        /// Create client adapter
        /// </summary>
        /// <param name="product"></param>
        /// <param name="onError"></param>
        /// <param name="transportSetting"></param>
        /// <returns></returns>
        private Task<IClient> CreateAdapterAsync(string product, Action onError,
            ITransportSettings transportSetting = null) {
            if (string.IsNullOrEmpty(ModuleId)) {
                if (_cs == null) {
                    throw new InvalidConfigurationException(
                        "No connection string for device client specified.");
                }
                return DeviceClientAdapter.CreateAsync(product, _cs, DeviceId,
                    transportSetting, _timeout, RetryPolicy, onError, _logger);
            }
            return ModuleClientAdapter.CreateAsync(product, _cs, DeviceId, ModuleId,
                transportSetting, _timeout, RetryPolicy, onError, _logger);
        }

        /// <summary>
        /// Adapts module client to interface
        /// </summary>
        public sealed class ModuleClientAdapter : IClient {

            /// <summary>
            /// Whether the client is closed
            /// </summary>
            public bool IsClosed { get; internal set; }

            /// <summary>
            /// Create client
            /// </summary>
            /// <param name="client"></param>
            internal ModuleClientAdapter(ModuleClient client) {
                _client = client ??
                    throw new ArgumentNullException(nameof(client));
            }

            /// <summary>
            /// Factory
            /// </summary>
            /// <param name="product"></param>
            /// <param name="cs"></param>
            /// <param name="deviceId"></param>
            /// <param name="moduleId"></param>
            /// <param name="transportSetting"></param>
            /// <param name="timeout"></param>
            /// <param name="retry"></param>
            /// <param name="onConnectionLost"></param>
            /// <param name="logger"></param>
            /// <returns></returns>
            public static async Task<IClient> CreateAsync(string product,
                IotHubConnectionStringBuilder cs, string deviceId, string moduleId,
                ITransportSettings transportSetting,
                TimeSpan timeout, IRetryPolicy retry, Action onConnectionLost,
                ILogger logger) {

                if (cs == null) {
                    logger.Information("Running in iotedge context.");
                }
                else {
                    logger.Information("Running outside iotedge context.");
                }

                var client = await CreateAsync(cs, transportSetting);
                var adapter = new ModuleClientAdapter(client);

                // Configure
                client.OperationTimeoutInMilliseconds = (uint)timeout.TotalMilliseconds;
                client.SetConnectionStatusChangesHandler((s, r) =>
                    adapter.OnConnectionStatusChange(deviceId, moduleId, onConnectionLost,
                        logger, s, r));
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
            public async Task SendEventBatchAsync(IEnumerable<Message> messages) {
                if (IsClosed) {
                    return;
                }
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
            public Task UploadToBlobAsync(string blobName, Stream source) {
                throw new NotSupportedException("Module client does not support upload");
            }

            /// <inheritdoc />
            public Task<MethodResponse> InvokeMethodAsync(string deviceId, string moduleId,
                MethodRequest methodRequest, CancellationToken cancellationToken) {
                return _client.InvokeMethodAsync(deviceId, moduleId, methodRequest, cancellationToken);
            }

            /// <inheritdoc />
            public Task<MethodResponse> InvokeMethodAsync(string deviceId,
                MethodRequest methodRequest, CancellationToken cancellationToken) {
                return _client.InvokeMethodAsync(deviceId, methodRequest, cancellationToken);
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
            /// <param name="moduleId"></param>
            /// <param name="onConnectionLost"></param>
            /// <param name="logger"></param>
            /// <param name="status"></param>
            /// <param name="reason"></param>
            private void OnConnectionStatusChange(string deviceId, string moduleId,
                Action onConnectionLost, ILogger logger, ConnectionStatus status,
                ConnectionStatusChangeReason reason) {

                if (status == ConnectionStatus.Connected) {
                    logger.Information("{counter}: Module {deviceId}_{moduleId} reconnected " +
                        "due to {reason}.", _reconnectCounter, deviceId, moduleId, reason);
                    kReconnectionStatus.WithLabels(moduleId, deviceId, DateTime.UtcNow.ToString()).Set(_reconnectCounter);
                    _reconnectCounter++;
                    return;
                }
                kDisconnectionStatus.WithLabels(moduleId, deviceId, DateTime.UtcNow.ToString()).Set(_reconnectCounter);
                logger.Information("{counter}: Module {deviceId}_{moduleId} disconnected " +
                    "due to {reason} - now {status}...", _reconnectCounter, deviceId, moduleId,
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
                        return await ModuleClient.CreateFromEnvironmentAsync();
                    }
                    return ModuleClient.CreateFromConnectionString(cs.ToString());
                }
                var ts = new ITransportSettings[] { transportSetting };
                if (cs == null) {
                    return await ModuleClient.CreateFromEnvironmentAsync(ts);
                }
                return ModuleClient.CreateFromConnectionString(cs.ToString(), ts);
            }

            private readonly ModuleClient _client;
            private int _reconnectCounter;
            private static readonly Gauge kReconnectionStatus = Metrics
                .CreateGauge("iiot_edge_reconnected", "reconnected count",
                    new GaugeConfiguration {
                        LabelNames = new[] { "module", "device", "timestamp_utc"}
                    });
            private static readonly Gauge kDisconnectionStatus = Metrics
                .CreateGauge("iiot_edge_disconnected", "reconnected count",
                    new GaugeConfiguration {
                        LabelNames = new[] { "module", "device", "timestamp_utc"}
                    });
        }

        /// <summary>
        /// Adapts device client to interface
        /// </summary>
        public sealed class DeviceClientAdapter : IClient {

            /// <summary>
            /// Whether the client is closed
            /// </summary>
            public bool IsClosed { get; internal set; }

            /// <summary>
            /// Create client
            /// </summary>
            /// <param name="client"></param>
            internal DeviceClientAdapter(DeviceClient client) {
                _client = client ??
                    throw new ArgumentNullException(nameof(client));
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
                var adapter = new DeviceClientAdapter(client);

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
            public async Task SendEventBatchAsync(IEnumerable<Message> messages) {
                if (IsClosed) {
                    return;
                }
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
                    kReconnectionStatus.WithLabels(deviceId, DateTime.UtcNow.ToString()).Set(_reconnectCounter);
                    _reconnectCounter++;
                    return;
                }
                logger.Information("{counter}: Device {deviceId} disconnected " +
                    "due to {reason} - now {status}...", _reconnectCounter, deviceId,
                        reason, status);
                kDisconnectionStatus.WithLabels(deviceId, DateTime.UtcNow.ToString()).Set(_reconnectCounter);
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
            private int _reconnectCounter;
            private static readonly Gauge kReconnectionStatus = Metrics
                .CreateGauge("iiot_edge_device_reconnected", "reconnected count",
                    new GaugeConfiguration {
                        LabelNames = new[] { "device", "timestamp_utc" }
                    });
            private static readonly Gauge kDisconnectionStatus = Metrics
                .CreateGauge("iiot_edge_device_disconnected", "disconnected count",
                    new GaugeConfiguration {
                        LabelNames = new[] { "device", "timestamp_utc" }
                    });
        }

        /// <summary>
        /// Add certificate in local cert store for use by client for secure connection
        /// to iotedge runtime
        /// </summary>
        private void InstallCert(string certPath) {
            if (!File.Exists(certPath)) {
                // We cannot proceed further without a proper cert file
                _logger.Error("Missing certificate file: {certPath}", certPath);
                throw new InvalidOperationException("Missing certificate file.");
            }

            var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            using (var cert = new X509Certificate2(X509Certificate.CreateFromCertFile(certPath))) {
                store.Add(cert);
            }
            _logger.Information("Added Cert: {certPath}", certPath);
            store.Close();
        }

        /// <summary>
        /// Sdk logger event source hook
        /// </summary>
        internal sealed class IoTSdkLogger : EventSourceSerilogSink {

            /// <inheritdoc/>
            public IoTSdkLogger(ILogger logger) :
                base(logger.ForContext("SourceContext", EventSource.Replace('-', '.'))) {
            }

            /// <inheritdoc/>
            public override void OnEvent(EventWrittenEventArgs eventData) {
                switch (eventData.EventName) {
                    case "Enter":
                    case "Exit":
                    case "Associate":
                        WriteEvent(LogEventLevel.Verbose, eventData);
                        break;
                    default:
                        WriteEvent(LogEventLevel.Debug, eventData);
                        break;
                }
            }

            // ddbee999-a79e-5050-ea3c-6d1a8a7bafdd
            public const string EventSource = "Microsoft-Azure-Devices-Device-Client";
        }

        private readonly TimeSpan _timeout;
        private readonly TransportOption _transport;
        private readonly IotHubConnectionStringBuilder _cs;
        private readonly ILogger _logger;
        private readonly IDisposable _logHook;
        private readonly bool _bypassCertValidation;
    }
}
