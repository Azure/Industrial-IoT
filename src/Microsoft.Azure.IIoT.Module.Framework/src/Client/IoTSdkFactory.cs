// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Microsoft.Azure.Devices.Shared;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using System.Threading;

    /// <summary>
    /// Create clients from device sdk
    /// </summary>
    public class IoTSdkFactory : IClientFactory {

        /// <inheritdoc />
        public string DeviceId { get; }

        /// <inheritdoc />
        public string ModuleId { get; }

        /// <inheritdoc />
        public IRetryPolicy RetryPolicy { get; set; }

        /// <summary>
        /// Create edge service module
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public IoTSdkFactory(IEdgeConfig config, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // The Edge runtime injects this as an environment variable
            var deviceId = Environment.GetEnvironmentVariable("IOTEDGE_DEVICEID");
            var moduleId = Environment.GetEnvironmentVariable("IOTEDGE_MODULEID");
            var ehubHost = Environment.GetEnvironmentVariable("IOTEDGE_GATEWAYHOSTNAME");
            try {
                if (!string.IsNullOrEmpty(config.HubConnectionString)) {
                    _cs = IotHubConnectionStringBuilder.Create(config.HubConnectionString);
                    if (string.IsNullOrEmpty(_cs.DeviceId)) {
                        throw new InvalidConfigurationException(
                            "Connection string is not a device or module connection string.");
                    }
                    deviceId = _cs.DeviceId;
                    moduleId = _cs.ModuleId;
                    ehubHost = _cs.GatewayHostName;
                }
                else if (string.IsNullOrEmpty(moduleId)) {
                    throw new InvalidConfigurationException(
                        "Must have connection string or module id to create clients.");
                }
            }
            catch (Exception e) {
                var ex = new InvalidConfigurationException(
                    "The host configuration is incomplete and is missing a " +
                    "connection string for Azure IoT Edge or IoT Hub. " +
                    "You either have to run the host under the control of the " +
                    "edge hub, or manually set the 'EdgeHubConnectionString' " +
                    "environment variable or configure the connection string " +
                    "value in your 'appsettings.json' configuration file.", e);
                _logger.Error("Bad configuration", () => ex);
                throw ex;
            }

            ModuleId = moduleId;
            DeviceId = deviceId;

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
            else if (!string.IsNullOrEmpty(ehubHost) || !string.IsNullOrEmpty(moduleId)) {
                if (ehubHost != null) {
                    // Running in edge context - can only use mqtt over tcp at this point
                    _transport = TransportOption.MqttOverTcp;
                }
                else {
                    // Directly connect module to cloud, only amqp works at this point.
                    _transport = TransportOption.Amqp;
                }
            }
            else {
                _transport = config.Transport;
            }

            _timeout = TimeSpan.FromMinutes(5);
            RetryPolicy = new ExponentialBackoff(1000,
                TimeSpan.FromSeconds(3), TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// Create and open a DeviceClient using the connection string
        /// </summary>
        /// <returns>Device client</returns>
        public async Task<IClient> CreateAsync() {

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
                         () => CreateAdapterAsync(t))
                    .ToArray());
            }
            return await CreateAdapterAsync();
        }

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="transportSetting"></param>
        /// <returns></returns>
        private Task<IClient> CreateAdapterAsync(
            ITransportSettings transportSetting = null) {
            if (_cs != null && string.IsNullOrEmpty(_cs.ModuleId)) {
                return DeviceClientAdapter.CreateAsync(_cs, transportSetting,
                    _timeout, RetryPolicy, _logger);
            }
            return ModuleClientAdapter.CreateAsync(_cs, transportSetting,
                _timeout, RetryPolicy, _logger);
        }

        /// <summary>
        /// Adapts module client to interface
        /// </summary>
        public sealed class ModuleClientAdapter : IClient {

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
            /// <param name="cs"></param>
            /// <param name="transportSetting"></param>
            /// <param name="timeout"></param>
            /// <param name="logger"></param>
            /// <returns></returns>
            public static async Task<IClient> CreateAsync(
                IotHubConnectionStringBuilder cs, ITransportSettings transportSetting,
                TimeSpan timeout, IRetryPolicy retry, ILogger logger) {

                var client = await CreateAsync(cs, transportSetting);

                // Configure
                client.OperationTimeoutInMilliseconds = (uint)timeout.TotalMilliseconds;
                client.SetConnectionStatusChangesHandler((s, r) =>
                    logger.Info($"Module connection status changed to {s} due to {r}.",
                        () => { }));
                if (retry != null) {
                    client.SetRetryPolicy(retry);
                }
                client.DiagnosticSamplingPercentage = 5;
                await client.OpenAsync();
                return new ModuleClientAdapter(client);
            }

            /// <inheritdoc />
            public string ProductInfo {
                get => _client.ProductInfo;
                set => _client.ProductInfo = value;
            }

            /// <inheritdoc />
            public Task CloseAsync() {
                _client.OperationTimeoutInMilliseconds = 3000;
                return _client.CloseAsync();
            }

            /// <inheritdoc />
            public Task SendEventAsync(Message message) =>
                _client.SendEventAsync(message);

            /// <inheritdoc />
            public Task SendEventBatchAsync(IEnumerable<Message> messages) =>
                _client.SendEventBatchAsync(messages);

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
                _client.UpdateReportedPropertiesAsync(reportedProperties);

            /// <inheritdoc />
            public Task UploadToBlobAsync(string blobName, Stream source) =>
                throw new NotSupportedException("Module client does not support upload");

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
            /// <param name="cs"></param>
            /// <param name="transportSetting"></param>
            /// <param name="timeout"></param>
            /// <param name="logger"></param>
            /// <returns></returns>
            public static async Task<IClient> CreateAsync(
                IotHubConnectionStringBuilder cs, ITransportSettings transportSetting,
                TimeSpan timeout, IRetryPolicy retry, ILogger logger) {
                var client = Create(cs, transportSetting);
                // Configure
                client.OperationTimeoutInMilliseconds = (uint)timeout.TotalMilliseconds;
                client.SetConnectionStatusChangesHandler((s, r) =>
                    logger.Info($"Device connection status changed to {s} due to {r}.",
                    () => { }));
                if (retry != null) {
                    client.SetRetryPolicy(retry);
                }
                client.DiagnosticSamplingPercentage = 5;
                await client.OpenAsync();
                return new DeviceClientAdapter(client);
            }

            /// <inheritdoc />
            public string ProductInfo {
                get => _client.ProductInfo;
                set => _client.ProductInfo = value;
            }

            /// <inheritdoc />
            public Task CloseAsync() {
                _client.OperationTimeoutInMilliseconds = 3000;
                return _client.CloseAsync();
            }

            /// <inheritdoc />
            public Task SendEventAsync(Message message) =>
                _client.SendEventAsync(message);

            /// <inheritdoc />
            public Task SendEventBatchAsync(IEnumerable<Message> messages) =>
                _client.SendEventBatchAsync(messages);

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
                _client.UpdateReportedPropertiesAsync(reportedProperties);

            /// <inheritdoc />
            public Task UploadToBlobAsync(string blobName, Stream source) =>
                _client.UploadToBlobAsync(blobName, source);

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
        /// to IoT Edge runtime
        /// </summary>
        private void InstallCert(string certPath) {
            if (!File.Exists(certPath)) {
                // We cannot proceed further without a proper cert file
                _logger.Error($"Missing certificate file: {certPath}", () => { });
                throw new InvalidOperationException("Missing certificate file.");
            }

            var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            store.Add(new X509Certificate2(X509Certificate.CreateFromCertFile(certPath)));
            _logger.Info("Added Cert: " + certPath, () => { });
            store.Close();
        }

        private readonly TimeSpan _timeout;
        private readonly TransportOption _transport;
        private readonly IotHubConnectionStringBuilder _cs;
        private readonly ILogger _logger;
        private readonly bool _bypassCertValidation;
    }
}
