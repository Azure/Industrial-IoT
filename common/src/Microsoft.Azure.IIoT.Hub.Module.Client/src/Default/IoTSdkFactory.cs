// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.Hub.Module.Client.Default
{
    using Microsoft.Azure.IIoT.Abstractions;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Microsoft.Azure.IIoT.Module.Framework.Client.MqttClient;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Microsoft.Extensions.Logging;
    using Furly.Exceptions;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    /// <summary>
    /// Injectable factory that creates clients
    /// </summary>
    public sealed class IoTSdkFactory : IClientFactory
    {
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
        /// <param name="logger"></param>
        public IoTSdkFactory(IModuleConfig config, ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _telemetryTopicTemplate = config.TelemetryTopicTemplate;

            // The runtime injects this as an environment variable
            var deviceId = Environment.GetEnvironmentVariable(IoTEdgeVariables.IOTEDGE_DEVICEID);
            var moduleId = Environment.GetEnvironmentVariable(IoTEdgeVariables.IOTEDGE_MODULEID);
            var ehubHost = Environment.GetEnvironmentVariable(IoTEdgeVariables.IOTEDGE_GATEWAYHOSTNAME);

            try
            {
                if (!string.IsNullOrEmpty(config.MqttClientConnectionString) &&
                    !string.IsNullOrEmpty(config.EdgeHubConnectionString))
                {
                    throw new InvalidConfigurationException(
                        "Can't have both a mqtt client connection string and a device connection string.");
                }

                if (!string.IsNullOrEmpty(config.MqttClientConnectionString))
                {
                    _mqttClientCs = MqttClientConnectionStringBuilder.Create(config.MqttClientConnectionString);

                    if (_mqttClientCs.UsingIoTHub && string.IsNullOrEmpty(_mqttClientCs.SharedAccessSignature))
                    {
                        throw new InvalidConfigurationException(
                            "Connection string is missing shared access key.");
                    }
                    if (_mqttClientCs.UsingIoTHub && string.IsNullOrEmpty(_mqttClientCs.DeviceId))
                    {
                        throw new InvalidConfigurationException(
                            "Connection string is missing device id.");
                    }

                    deviceId = _mqttClientCs.DeviceId;
                    moduleId = _mqttClientCs.ModuleId;
                    _timeout = TimeSpan.FromSeconds(15);
                }
                else if (!string.IsNullOrEmpty(config.EdgeHubConnectionString))
                {
                    _deviceClientCs = IotHubConnectionStringBuilder.Create(config.EdgeHubConnectionString);

                    if (string.IsNullOrEmpty(_deviceClientCs.SharedAccessKey))
                    {
                        throw new InvalidConfigurationException(
                            "Connection string is missing shared access key.");
                    }
                    if (string.IsNullOrEmpty(_deviceClientCs.DeviceId))
                    {
                        throw new InvalidConfigurationException(
                            "Connection string is missing device id.");
                    }

                    deviceId = _deviceClientCs.DeviceId;
                    moduleId = _deviceClientCs.ModuleId;
                    ehubHost = _deviceClientCs.GatewayHostName ?? ehubHost;

                    if (string.IsNullOrWhiteSpace(_deviceClientCs.GatewayHostName) && !string.IsNullOrWhiteSpace(ehubHost))
                    {
                        _deviceClientCs = IotHubConnectionStringBuilder.Create(
                            config.EdgeHubConnectionString + ";GatewayHostName=" + ehubHost);

                        _logger.LogInformation("Details of gateway host are added to IoT Hub connection string: " +
                            "GatewayHostName={Host}", ehubHost);
                    }
                    _timeout = TimeSpan.FromMinutes(5);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Bad configuration value in EdgeHubConnectionString config.");
            }

            ModuleId = moduleId;
            DeviceId = deviceId;
            Gateway = ehubHost;

            if (string.IsNullOrEmpty(config.MqttClientConnectionString) && string.IsNullOrEmpty(DeviceId))
            {
                var ex = new InvalidConfigurationException(
                    "If you are running outside of an IoT Edge context or in EdgeHubDev mode, then the " +
                    "host configuration is incomplete and missing the EdgeHubConnectionString setting." +
                    "You can run the module using the command line interface or in IoT Edge context, or " +
                    "manually set the 'EdgeHubConnectionString' environment variable.");

                _logger.LogError(ex, "The sdk factory was not configured correctly. Device Id is missing.");
                throw ex;
            }

            _bypassCertValidation = config.BypassCertVerification;
            if (!_bypassCertValidation)
            {
                var certPath = Environment.GetEnvironmentVariable("EdgeModuleCACertificateFile");
                if (!string.IsNullOrWhiteSpace(certPath))
                {
                    InstallCert(certPath);
                }
                else if (!string.IsNullOrEmpty(ehubHost))
                {
                    _bypassCertValidation = true;
                }
            }
            _enableOutputRouting = config.EnableOutputRouting;

            if (!string.IsNullOrEmpty(ehubHost))
            {
                // Running in edge mode
                // the configured transport (if provided) will be forced to it's OverTcp
                // variant as follows: AmqpOverTcp when Amqp, AmqpOverWebsocket or AmqpOverTcp specified
                // and MqttOverTcp otherwise. Default is MqttOverTcp
                if ((config.Transport & TransportOption.Mqtt) != 0)
                {
                    // prefer Mqtt over Amqp due to performance reasons
                    _transport = TransportOption.MqttOverTcp;
                }
                else
                {
                    _transport = TransportOption.AmqpOverTcp;
                }
                _logger.LogInformation("Connecting all clients to {EdgeHub} using {Transport}.",
                    ehubHost, _transport);
            }
            else
            {
                _transport = config.Transport;
            }
        }

        /// <inheritdoc/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5359:Do Not Disable Certificate Validation",
            Justification = "<Pending>")]
        public async Task<IClient> CreateAsync(string product,
            IMetricsContext metrics, IProcessControl ctrl)
        {
            if (_bypassCertValidation)
            {
                _logger.LogWarning("Bypassing certificate validation for client.");
            }

            // Configure transport settings
            var transportSettings = new List<ITransportSettings>();

            if ((_transport & TransportOption.MqttOverTcp) != 0)
            {
                var setting = new MqttTransportSettings(
                    TransportType.Mqtt_Tcp_Only);
                if (_bypassCertValidation)
                {
                    setting.RemoteCertificateValidationCallback =
                        (sender, certificate, chain, sslPolicyErrors) => true;
                }
                transportSettings.Add(setting);
            }
            if ((_transport & TransportOption.MqttOverWebsocket) != 0)
            {
                var setting = new MqttTransportSettings(
                    TransportType.Mqtt_WebSocket_Only);
                if (_bypassCertValidation)
                {
                    setting.RemoteCertificateValidationCallback =
                        (sender, certificate, chain, sslPolicyErrors) => true;
                }
                transportSettings.Add(setting);
            }
            if ((_transport & TransportOption.AmqpOverTcp) != 0)
            {
                var setting = new AmqpTransportSettings(
                    TransportType.Amqp_Tcp_Only);
                if (_bypassCertValidation)
                {
                    setting.RemoteCertificateValidationCallback =
                        (sender, certificate, chain, sslPolicyErrors) => true;
                }
                transportSettings.Add(setting);
            }
            if ((_transport & TransportOption.AmqpOverWebsocket) != 0)
            {
                var setting = new AmqpTransportSettings(
                    TransportType.Amqp_WebSocket_Only);
                if (_bypassCertValidation)
                {
                    setting.RemoteCertificateValidationCallback =
                        (sender, certificate, chain, sslPolicyErrors) => true;
                }
                transportSettings.Add(setting);
            }
            if (transportSettings.Count != 0)
            {
                return await TryAll(transportSettings
                    .Select<ITransportSettings, Func<Task<IClient>>>(t =>
                         () => CreateAdapterAsync(product, () => ctrl?.Reset(), metrics, t))
                    .ToArray()).ConfigureAwait(false);
            }
            return await CreateAdapterAsync(product, () => ctrl?.Reset(), metrics).ConfigureAwait(false);

            static async Task<T> TryAll<T>(params Func<Task<T>>[] options)
            {
                var exceptions = new List<Exception>();
                foreach (var option in options)
                {
                    try
                    {
                        return await option().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }
                throw new AggregateException(exceptions);
            }
        }

        /// <summary>
        /// Create client adapter
        /// </summary>
        /// <param name="product"></param>
        /// <param name="onError"></param>
        /// <param name="metrics"></param>
        /// <param name="transportSetting"></param>
        /// <returns></returns>
        /// <exception cref="InvalidConfigurationException"></exception>
        private Task<IClient> CreateAdapterAsync(string product, Action onError,
            IMetricsContext metrics, ITransportSettings transportSetting = null)
        {
            if (string.IsNullOrEmpty(ModuleId))
            {
                if (_mqttClientCs != null)
                {
                    return MqttClientAdapter.CreateAsync(_mqttClientCs, DeviceId, _telemetryTopicTemplate,
                        timeout: _timeout, logger: _logger, metrics: metrics);
                }
                else if (_deviceClientCs != null)
                {
                    return DeviceClientAdapter.CreateAsync(product, _deviceClientCs, DeviceId,
                        transportSetting, timeout: _timeout, RetryPolicy, onError, _logger, metrics);
                }
                else
                {
                    throw new InvalidConfigurationException(
                        "No connection string for device client specified.");
                }
            }
            return ModuleClientAdapter.CreateAsync(product, _deviceClientCs, DeviceId, ModuleId,
                _enableOutputRouting, transportSetting, timeout: _timeout, retry: RetryPolicy,
                onConnectionLost: onError, logger: _logger, metrics: metrics);
        }

        /// <summary>
        /// Add certificate in local cert store for use by client for secure connection
        /// to iotedge runtime
        /// </summary>
        /// <param name="certPath"></param>
        /// <exception cref="InvalidOperationException"></exception>
        private void InstallCert(string certPath)
        {
            if (!File.Exists(certPath))
            {
                // We cannot proceed further without a proper cert file
                _logger.LogError("Missing certificate file: {CertPath}", certPath);
                throw new InvalidOperationException("Missing certificate file.");
            }

            var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            using (var cert = new X509Certificate2(X509Certificate.CreateFromCertFile(certPath)))
            {
                store.Add(cert);
            }
            _logger.LogInformation("Added Cert: {CertPath}", certPath);
            store.Close();
        }

        private readonly TimeSpan _timeout;
        private readonly TransportOption _transport;
        private readonly IotHubConnectionStringBuilder _deviceClientCs;
        private readonly MqttClientConnectionStringBuilder _mqttClientCs;
        private readonly ILogger _logger;
        private readonly string _telemetryTopicTemplate;
        private readonly bool _bypassCertValidation;
        private readonly bool _enableOutputRouting;
    }
}
