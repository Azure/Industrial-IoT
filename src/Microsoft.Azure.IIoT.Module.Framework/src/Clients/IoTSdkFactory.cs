// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client {
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Microsoft.Azure.Devices.Shared;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using System.Threading;

    /// <summary>
    /// Injectable factory that creates clients from device sdk
    /// </summary>
    public sealed class IoTSdkFactory : IClientFactory {

        /// <inheritdoc />
        public string DeviceId { get; }

        /// <inheritdoc />
        public string ModuleId { get; }

        /// <inheritdoc />
        public IRetryPolicy RetryPolicy { get; set; }

        /// <summary>
        /// Create sdk factory
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public IoTSdkFactory(IModuleConfig config, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // The runtime injects this as an environment variable
            var deviceId = Environment.GetEnvironmentVariable("IOTEDGE_DEVICEID");
            var moduleId = Environment.GetEnvironmentVariable("IOTEDGE_MODULEID");
            var ehubHost = Environment.GetEnvironmentVariable("IOTEDGE_GATEWAYHOSTNAME");

            if (string.IsNullOrEmpty(deviceId) ||
                string.IsNullOrEmpty(moduleId)) {
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
                        ehubHost = _cs.GatewayHostName;
                    }
                }
                catch (Exception e) {
                    _logger.Error(e, "Bad configuration value in EdgeHubConnectionString config.");
                }
            }

            ModuleId = moduleId;
            DeviceId = deviceId;

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
            }

            if (_bypassCertValidation) {
                // Running in debug mode - can only use mqtt over tcp
                _transport = TransportOption.MqttOverTcp;
            }
            else {
                _transport = config.Transport;
            }

            _timeout = TimeSpan.FromMinutes(5);
        }

        /// <inheritdoc/>
        public async Task<IClient> CreateAsync(string product, IProcessControl ctrl) {

            // Configure transport settings
            var transportSettings = new List<ITransportSettings>();

            if ((_transport & TransportOption.Mqtt) != 0) {
                if ((_transport & TransportOption.MqttOverTcp) != 0) {
                    var setting = new MqttTransportSettings(
                        TransportType.Mqtt_Tcp_Only);
                    if (_bypassCertValidation) {
                        setting.RemoteCertificateValidationCallback =
                            (sender, certificate, chain, sslPolicyErrors) => true;
                    }
                    transportSettings.Add(setting);
                }
                else {
                    transportSettings.Add(new MqttTransportSettings(
                        TransportType.Mqtt_WebSocket_Only));
                }
            }

            if ((_transport & TransportOption.Amqp) != 0) {
                if ((_transport & TransportOption.AmqpOverTcp) != 0) {
                    transportSettings.Add(new AmqpTransportSettings(
                        TransportType.Amqp_Tcp_Only));
                }
                else {
                    transportSettings.Add(new AmqpTransportSettings(
                        TransportType.Amqp_WebSocket_Only));
                }
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
                    logger.Information("Running in iotedge production context.");
                }
                else {
                    logger.Information("Running in iotedge development context.");
                }

                var client = await CreateAsync(cs, transportSetting);
                var adapter = new ModuleClientAdapter(client);

                // Configure
                client.OperationTimeoutInMilliseconds = (uint)timeout.TotalMilliseconds;
                client.SetConnectionStatusChangesHandler((s, r) => {
                    logger.Information("Module {deviceId}_{moduleId} connection status " +
                        "changed to {s} due to {r}.", deviceId, moduleId, s, r);
                    if (r == ConnectionStatusChangeReason.Client_Close && !adapter.IsClosed) {
                        adapter.IsClosed = true;
                        onConnectionLost?.Invoke();
                    }
                });
                if (retry != null) {
                    client.SetRetryPolicy(retry);
                }
                client.DiagnosticSamplingPercentage = 5;
                client.ProductInfo = product;
                await client.OpenAsync();
                return adapter;
            }

            /// <inheritdoc />
            public Task CloseAsync() {
                _client.OperationTimeoutInMilliseconds = 3000;
                _client.SetRetryPolicy(new NoRetry());
                return IsClosed ? Task.CompletedTask : _client.CloseAsync();
            }

            /// <inheritdoc />
            public Task SendEventAsync(Message message) =>
                IsClosed ? Task.CompletedTask : _client.SendEventAsync(message);

            /// <inheritdoc />
            public Task SendEventBatchAsync(IEnumerable<Message> messages) =>
                IsClosed ? Task.CompletedTask : _client.SendEventBatchAsync(messages);

            /// <inheritdoc />
            public Task SetMethodHandlerAsync(string methodName,
                MethodCallback methodHandler, object userContext) =>
                _client.SetMethodHandlerAsync(methodName, methodHandler, userContext);

            /// <inheritdoc />
            public Task SetMethodDefaultHandlerAsync(
                MethodCallback methodHandler, object userContext) =>
                _client.SetMethodDefaultHandlerAsync(methodHandler, userContext);

            /// <inheritdoc />
            public Task SetDesiredPropertyUpdateCallbackAsync(
                DesiredPropertyUpdateCallback callback, object userContext) =>
                _client.SetDesiredPropertyUpdateCallbackAsync(callback, userContext);

            /// <inheritdoc />
            public Task<Twin> GetTwinAsync() =>
                _client.GetTwinAsync();

            /// <inheritdoc />
            public Task UpdateReportedPropertiesAsync(TwinCollection reportedProperties) =>
                IsClosed? Task.CompletedTask : _client.UpdateReportedPropertiesAsync(reportedProperties);

            /// <inheritdoc />
            public Task UploadToBlobAsync(string blobName, Stream source) =>
                throw new NotSupportedException("Module client does not support upload");

            /// <inheritdoc />
            public Task<MethodResponse> InvokeMethodAsync(string deviceId, string moduleId,
                MethodRequest methodRequest, CancellationToken cancellationToken) =>
                _client.InvokeMethodAsync(deviceId, moduleId, methodRequest, cancellationToken);

            /// <inheritdoc />
            public Task<MethodResponse> InvokeMethodAsync(string deviceId,
                MethodRequest methodRequest, CancellationToken cancellationToken) =>
                _client.InvokeMethodAsync(deviceId, methodRequest, cancellationToken);

            /// <inheritdoc />
            public Task SetStreamsDefaultHandlerAsync(StreamCallback streamHandler,
                object userContext) =>
                throw new NotSupportedException("Module client does not support streams");

            /// <inheritdoc />
            public Task SetStreamHandlerAsync(string streamName, StreamCallback
                streamHandler, object userContext) =>
                throw new NotSupportedException("Module client does not support streams");

            /// <inheritdoc />
            public Task<Stream> CreateStreamAsync(string streamName, string hostName,
                ushort port, CancellationToken cancellationToken) =>
                throw new NotSupportedException("Module client does not support streams");

            /// <inheritdoc />
            public void Dispose() =>
                _client?.Dispose();

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
                client.SetConnectionStatusChangesHandler((s, r) => {
                    logger.Information(
                        "Device {deviceId} connection status changed to {s} due to {r}.",
                        deviceId, s, r);

                    if (r == ConnectionStatusChangeReason.Client_Close && !adapter.IsClosed) {
                        adapter.IsClosed = true;
                        onConnectionLost?.Invoke();
                    }
                });
                if (retry != null) {
                    client.SetRetryPolicy(retry);
                }
                client.DiagnosticSamplingPercentage = 5;
                client.ProductInfo = product;

                await client.OpenAsync();
                return adapter;
            }

            /// <inheritdoc />
            public Task CloseAsync() {
                _client.OperationTimeoutInMilliseconds = 3000;
                _client.SetRetryPolicy(new NoRetry());
                return IsClosed ? Task.CompletedTask : _client.CloseAsync();
            }

            /// <inheritdoc />
            public Task SendEventAsync(Message message) =>
                IsClosed ? Task.CompletedTask : _client.SendEventAsync(message);

            /// <inheritdoc />
            public Task SendEventBatchAsync(IEnumerable<Message> messages) =>
                IsClosed ? Task.CompletedTask : _client.SendEventBatchAsync(messages);

            /// <inheritdoc />
            public Task SetMethodHandlerAsync(string methodName,
                MethodCallback methodHandler, object userContext) =>
                _client.SetMethodHandlerAsync(methodName, methodHandler, userContext);

            /// <inheritdoc />
            public Task SetMethodDefaultHandlerAsync(
                MethodCallback methodHandler, object userContext) =>
                _client.SetMethodDefaultHandlerAsync(methodHandler, userContext);

            /// <inheritdoc />
            public Task SetDesiredPropertyUpdateCallbackAsync(
                DesiredPropertyUpdateCallback callback, object userContext) =>
                _client.SetDesiredPropertyUpdateCallbackAsync(callback, userContext);

            /// <inheritdoc />
            public Task<Twin> GetTwinAsync() =>
                _client.GetTwinAsync();

            /// <inheritdoc />
            public Task UpdateReportedPropertiesAsync(TwinCollection reportedProperties) =>
                IsClosed ? Task.CompletedTask : _client.UpdateReportedPropertiesAsync(reportedProperties);

            /// <inheritdoc />
            public Task UploadToBlobAsync(string blobName, Stream source) =>
                IsClosed ? Task.CompletedTask : _client.UploadToBlobAsync(blobName, source);

            /// <inheritdoc />
            public Task<MethodResponse> InvokeMethodAsync(string deviceId, string moduleId,
                MethodRequest methodRequest, CancellationToken cancellationToken) =>
                throw new NotSupportedException("Device client does not support methods");

            /// <inheritdoc />
            public Task<MethodResponse> InvokeMethodAsync(string deviceId,
                MethodRequest methodRequest, CancellationToken cancellationToken) =>
                throw new NotSupportedException("Device client does not support methods");

            /// <inheritdoc />
            public Task SetStreamsDefaultHandlerAsync(StreamCallback streamHandler,
                object userContext) =>
                throw new NotSupportedException("Device client does not support streams");

            /// <inheritdoc />
            public Task SetStreamHandlerAsync(string streamName, StreamCallback
                streamHandler, object userContext) =>
                throw new NotSupportedException("Device client does not support streams");

            /// <inheritdoc />
            public Task<Stream> CreateStreamAsync(string streamName, string hostName,
                ushort port, CancellationToken cancellationToken) =>
                throw new NotSupportedException("Device client does not support streams");

            /// <inheritdoc />
            public void Dispose() =>
                _client?.Dispose();

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

            var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            store.Add(new X509Certificate2(X509Certificate.CreateFromCertFile(certPath)));
            _logger.Information("Added Cert: {certPath}", certPath);
            store.Close();
        }

        private readonly TimeSpan _timeout;
        private readonly TransportOption _transport;
        private readonly IotHubConnectionStringBuilder _cs;
        private readonly ILogger _logger;
        private readonly bool _bypassCertValidation;
    }
}
