// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client {
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Serilog;
    using Serilog.Events;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using System.Diagnostics.Tracing;
    using System.Runtime.CompilerServices;
    using System.Collections.Concurrent;

    /// <summary>
    /// Injectable factory that creates clients from device sdk
    /// </summary>
    public sealed partial class IoTSdkFactory : IClientFactory, IDisposable {

        private readonly ConcurrentDictionary<string, IClient> _clients = new ConcurrentDictionary<string, IClient>();

        /// <inheritdoc />
        private string EnvironmentDeviceId { get; }

        /// <inheritdoc />
        private string EnvironmentModuleId { get; }

        private string EnvironmentGateway { get; }

        /// <inheritdoc />
        public IRetryPolicy RetryPolicy { get; set; }

        /// <summary>
        /// Create sdk factory
        /// </summary>
        /// <param name="broker"></param>
        /// <param name="logger"></param>
        public IoTSdkFactory(IEventSourceBroker broker, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (broker != null) {
                _logHook = broker.Subscribe(IoTSdkLogger.EventSource, new IoTSdkLogger(logger));
            }

            // The runtime injects this as an environment variable
            EnvironmentDeviceId = Environment.GetEnvironmentVariable("IOTEDGE_DEVICEID");
            EnvironmentModuleId = Environment.GetEnvironmentVariable("IOTEDGE_MODULEID");
            EnvironmentGateway = Environment.GetEnvironmentVariable("IOTEDGE_GATEWAYHOSTNAME");

            _timeout = TimeSpan.FromMinutes(5);
        }

        /// <inheritdoc/>
        public void Dispose() {
            foreach(var client in _clients.Values) {
                client.Dispose();
            }

            _logHook?.Dispose();
        }

        private string GetClientId(string deviceId, string moduleId) {
            return deviceId + moduleId;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <returns></returns>
        public Task DisposeClient(string deviceId, string moduleId) {
            var clientKey = GetClientId(deviceId, moduleId);

            if (_clients.TryGetValue(clientKey, out var client)) {
                IClient removed;
                _clients.TryRemove(clientKey, out removed);
                client.Dispose();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<IClient> CreateAsync(IModuleConfig config, string product, IProcessControl ctrl) {

            IotHubConnectionStringBuilder csb = null;
            string deviceId = EnvironmentDeviceId;
            string moduleId = EnvironmentModuleId;
            string gateway = EnvironmentGateway;
            var bypassCertValidation = config?.BypassCertVerification ?? false;
            TransportOption transport;

            try {
                if (!string.IsNullOrEmpty(config?.EdgeHubConnectionString)) {
                    csb = IotHubConnectionStringBuilder.Create(config.EdgeHubConnectionString);

                    if (string.IsNullOrEmpty(csb.SharedAccessKey)) {
                        throw new InvalidConfigurationException(
                            "Connection string is missing shared access key.");
                    }
                    if (string.IsNullOrEmpty(csb.DeviceId)) {
                        throw new InvalidConfigurationException(
                            "Connection string is missing device id.");
                    }

                    deviceId = csb.DeviceId;
                    moduleId = csb.ModuleId;
                    gateway = csb.GatewayHostName;
                }
            }
            catch (Exception e) {
                _logger.Error(e, "Bad configuration value in EdgeHubConnectionString config.");
            }

            if (string.IsNullOrEmpty(deviceId)) {
                var ex = new InvalidConfigurationException(
                    "If you are running outside of an IoT Edge context or in EdgeHubDev mode, then the " +
                    "host configuration is incomplete and missing the EdgeHubConnectionString setting." +
                    "You can run the module using the command line interface or in IoT Edge context, or " +
                    "manually set the 'EdgeHubConnectionString' environment variable.");

                _logger.Error(ex, "The Twin module was not configured correctly.");
                throw ex;
            }

            var clientKey = GetClientId(deviceId, moduleId);

            if (_clients.ContainsKey(clientKey)) {
                return _clients[clientKey];
            }

            IClient client = null;

            if (!bypassCertValidation) {
                var certPath = Environment.GetEnvironmentVariable("EdgeModuleCACertificateFile");
                if (!string.IsNullOrWhiteSpace(certPath)) {
                    InstallCert(certPath);
                }
            }

            if (bypassCertValidation) {
                // Running in debug mode - can only use mqtt over tcp
                transport = TransportOption.MqttOverTcp;
            }
            else {
                transport = config.Transport;
            }

            // Configure transport settings
            var transportSettings = new List<ITransportSettings>();

            if ((transport & TransportOption.MqttOverTcp) != 0) {
                var setting = new MqttTransportSettings(
                    TransportType.Mqtt_Tcp_Only);
                if (config.BypassCertVerification) {
                    setting.RemoteCertificateValidationCallback =
                        (sender, certificate, chain, sslPolicyErrors) => true;
                }
                transportSettings.Add(setting);
            }
            if ((transport & TransportOption.MqttOverWebsocket) != 0) {
                transportSettings.Add(new MqttTransportSettings(
                    TransportType.Mqtt_WebSocket_Only));
            }
            if ((transport & TransportOption.AmqpOverTcp) != 0) {
                transportSettings.Add(new AmqpTransportSettings(
                    TransportType.Amqp_Tcp_Only));
            }
            if ((transport & TransportOption.AmqpOverWebsocket) != 0) {
                transportSettings.Add(new AmqpTransportSettings(
                    TransportType.Amqp_WebSocket_Only));
            }
            if (transportSettings.Count != 0) {
                client = await Try.Options(transportSettings
                    .Select<ITransportSettings, Func<Task<IClient>>>(t =>
                         () => CreateAdapterAsync(csb, deviceId, moduleId, product, () => ctrl?.Reset(), t))
                    .ToArray());
            }
            client = await CreateAdapterAsync(csb, deviceId, moduleId, product, () => ctrl?.Reset());

            _clients[clientKey] = client;
            return client;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cs"></param>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="product"></param>
        /// <param name="onError"></param>
        /// <param name="transportSetting"></param>
        /// <returns></returns>
        private Task<IClient> CreateAdapterAsync(IotHubConnectionStringBuilder cs, string deviceId, string moduleId, string product, Action onError,
            ITransportSettings transportSetting = null) {
            if (string.IsNullOrEmpty(cs?.ModuleId)) {
                if (cs == null) {
                    throw new InvalidConfigurationException(
                        "No connection string for device client specified.");
                }
                return DeviceClientAdapter.CreateAsync(product, cs, deviceId,
                    transportSetting, _timeout, RetryPolicy, onError, _logger);
            }
            return ModuleClientAdapter.CreateAsync(product, cs, deviceId, moduleId,
                transportSetting, _timeout, RetryPolicy, onError, _logger);
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
        private readonly ILogger _logger;
        private readonly IDisposable _logHook;
    }
}
