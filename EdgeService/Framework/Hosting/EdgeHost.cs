// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.Devices.Edge.Hosting {
    using Microsoft.Azure.Devices.Edge.Services;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Microsoft.Azure.Devices.Shared;
    using Microsoft.Azure.IoTSolutions.Common.Diagnostics;
    using Microsoft.Azure.IoTSolutions.Common.Exceptions;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Edge host implementation
    /// </summary>
    public class EdgeHost : IEdgeHost, ITwinProperties, IEventEmitter {

        /// <summary>
        /// Twin tags
        /// </summary>
        public IReadOnlyDictionary<string, dynamic> Reported => _reported;

        /// <summary>
        /// Device id events are emitted on
        /// </summary>
        public string DeviceId { get; private set; }

        /// <summary>
        /// Create edge service module
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        /// <param name="router"></param>
        public EdgeHost(IMethodRouter router, ISettingsRouter settings, IHostConfig config,
            ILogger logger) {
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            var certPath = Environment.GetEnvironmentVariable("EdgeModuleCACertificateFile");
            if (!string.IsNullOrWhiteSpace(certPath)) {
                InstallCert(certPath);
            }
        }

        /// <summary>
        /// Stop service
        /// </summary>
        public async Task StopAsync() {
            if (_client != null) {
                try {
                    await _lock.WaitAsync();
                    if (_client != null) {
                        await _client.CloseAsync();
                    }
                    _client = null;
                    _reported.Clear();
                }
                finally {
                    _lock.Release();
                }
            }
        }

        /// <summary>
        /// Start service
        /// </summary>
        /// <returns></returns>
        public async Task StartAsync() {
            if (_client == null) {
                try {
                    await _lock.WaitAsync();
                    if (_client == null) {
                        // Create client
                        _client = await CreateDeviceClientAsync();

                        // Register callback to be called when a method request is received
                        await _client.SetMethodDefaultHandlerAsync((request, _) => {
                            return _router.InvokeMethodAsync(request);
                        }, null);

                        await InitializeTwinAsync();

                        // Register callback to be called when settings change - updates settings...
                        await _client.SetDesiredPropertyUpdateCallbackAsync(async (settings, _) => {
                            await ProcessSettingsAsync(settings);
                        }, null);

                        // Done...
                        return;
                    }
                }
                catch (Exception ex) {
                    _client?.Dispose();
                    _client = null;
                    DeviceId = null;
                    throw ex;
                }
                finally {
                    _lock.Release();
                }
            }
            throw new InvalidOperationException("Already started");
        }

        /// <summary>
        /// Refresh twin
        /// </summary>
        /// <returns></returns>
        public async Task RefreshAsync() {
            try {
                await _lock.WaitAsync();
                if (_client != null) {
                    _twin = await _client.GetTwinAsync();
                    _reported.Clear();
                    foreach (KeyValuePair<string, dynamic> property in _twin.Properties.Reported) {
                        _reported.Add(property.Key, property.Value);
                    }
                    _client.Dispose();
                    DeviceId = null;
                 }
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Send events as batch
        /// </summary>
        /// <returns></returns>
        public async Task SendAsync(IEnumerable<byte[]> batch, string contentType) {
            try {
                await _lock.WaitAsync();
                if (_client != null) {
                    await _client.SendEventBatchAsync(batch.Select(ev =>
                        CreateMessage(ev, contentType)));
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Send event
        /// </summary>
        /// <returns></returns>
        public async Task SendAsync(byte[] data, string contentType) {
            try {
                await _lock.WaitAsync();
                if (_client != null) {
                    await _client.SendEventAsync(
                        CreateMessage(data, contentType));
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Send a list of updated property
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        public async Task SendAsync(IEnumerable<KeyValuePair<string, dynamic>> properties) {
            try {
                await _lock.WaitAsync();
                if (_client != null) {
                    var collection = new TwinCollection();
                    foreach(var property in properties) {
                        collection[property.Key] = property.Value;
                    }
                    await _client.UpdateReportedPropertiesAsync(collection);
                    foreach (var property in properties) {
                        _reported.Remove(property.Key, out var old);
                        if (old != null) {
                            _reported.Add(property.Key,
                                ((JToken)old).Apply((JToken)property.Value));
                        }
                        else {
                            _reported.Add(property.Key, property.Value);
                        }
                    }
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Send an updated property
        /// </summary>
        /// <param name="propertyId"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task SendAsync(string propertyId, dynamic value) {
            try {
                await _lock.WaitAsync();
                if (_client != null) {
                    var collection = new TwinCollection {
                        [propertyId] = value
                    };
                    await _client.UpdateReportedPropertiesAsync(collection);
                    _reported.Remove(propertyId, out var old);
                    if (old != null) {
                        value = ((JToken)old).Apply((JToken)value);
                    }
                    _reported.Add(propertyId, value);
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose() {
            if (_client != null) {
                StopAsync().Wait();
            }
        }

        /// <summary>
        /// Create pcs/central conform message
        /// </summary>
        /// <param name="data"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        private static Message CreateMessage(byte[] data, string contentType) {
            var msg = new Message(data) {
                ContentType = contentType,
                CreationTimeUtc = DateTime.UtcNow
            };
            if (!string.IsNullOrEmpty(contentType)) {
                msg.Properties.Add(kContentTypeKey, contentType);
            }
            msg.Properties.Add(kCreationTimeUtcKey, msg.CreationTimeUtc.ToString());
            return msg;
        }
        private const string kMessageSchemaKey = "$$MessageSchema";
        private const string kContentTypeKey = "$$ContentType";
        private const string kCreationTimeUtcKey = "$$CreationTimeUtc";

        /// <summary>
        /// Reads the twin including desired and reported settings and applies them to the
        /// settings controllers.  updates the twin for any changes resulting from the
        /// update.  Reported values are cached until user calls Refresh.
        /// </summary>
        /// <returns></returns>
        private async Task InitializeTwinAsync() {

            Contract.Invariant(_lock.CurrentCount == 0);

            // Process initial setting snapshot from twin
            _twin = await _client.GetTwinAsync();
            if (_twin.Properties.Reported.Count > 0 || _twin.Properties.Desired.Count > 0) {

                var desired = new TwinCollection();
                var reported = new TwinCollection();

                // Start with reported values which we desire to be applied
                foreach (KeyValuePair<string, dynamic> property in _twin.Properties.Reported) {
                    if (property.Value is JObject obj &&
                        obj.TryGetValue("Status", out var val) &&
                        obj.Children().Count() == 1) {
                        // Clear status properties from twin
                        reported[property.Key] = null;
                        continue;
                    }
                    desired[property.Key] = property.Value;
                    _reported.Add(property.Key, property.Value);
                }
                // Apply desired values on top.
                foreach (KeyValuePair<string, dynamic> property in _twin.Properties.Desired) {
                    desired[property.Key] = property.Value;
                }

                // Process settings on controllers
                _logger.Debug("Applying initial state.", () => desired);
                var processed = await _settings.ProcessSettingsAsync(desired);

                // If there are changes, update what should be reported back.
                foreach (KeyValuePair<string, dynamic> property in processed) {
                    if (!_twin.Properties.Reported.Contains(property.Key) && property.Value != null) {
                        reported[property.Key] = property.Value;
                    }
                    else if (!_twin.Properties.Reported[property.Key].Equals(property.Value)) {
                        reported[property.Key] = property.Value;
                    }
                }
                if (reported.Count > 0) {
                    _logger.Debug("Reporting initial state.", () => reported);
                    await _client.UpdateReportedPropertiesAsync(reported);
                }
            }
        }

        /// <summary>
        /// Update device client settings
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        private async Task ProcessSettingsAsync(TwinCollection settings) {
            if (settings.Count > 0) {
                try {
                    await _lock.WaitAsync();
                    // Patch existing reported properties
                    var patched = new TwinCollection();
                    foreach(KeyValuePair<string, dynamic> item in settings) {
                        if (!_reported.ContainsKey(item.Key)) {
                            patched[item.Key] = item.Value;
                        }
                        else {
                            var existing = (JToken)_reported[item.Key];
                            var updated = existing.Apply((JToken)item.Value);
                            patched[item.Key] = updated;
                        }
                    }
                    _logger.Debug("Applying state.", () => patched);
                    var reported = await _settings.ProcessSettingsAsync(patched);
                    if (reported != null && reported.Count != 0) {
                        _logger.Debug("Reporting state.", () => reported);
                        await _client.UpdateReportedPropertiesAsync(reported);
                        foreach (KeyValuePair<string, dynamic> property in reported) {
                            _reported.Remove(property.Key, out var old);
                            _reported.Add(property.Key, property.Value);
                        }
                    }
                }
                finally {
                    _lock.Release();
                }
            }
        }

        /// <summary>
        /// Create and open a DeviceClient using the connection string
        /// </summary>
        /// <returns>Device client</returns>
        private async Task<DeviceClient> CreateDeviceClientAsync() {

            // The Edge runtime injects this as an environment variable
            var connectionString = _config.HubConnectionString;

            if (string.IsNullOrEmpty(connectionString) ||
                connectionString.ToLowerInvariant().Contains("your azure iot hub")) {
                var ex = new InvalidConfigurationException(
                    "The host configuration is incomplete and is missing a " +
                    "connection string for Azure IoT Edge or IoT Hub. " +
                    "You either have to run the host under the control of the " +
                    "edge hub, or manually set the 'EdgeHubConnectionString' environment" +
                    "variable or connection string value in the 'appsettings.json' file.");
                _logger.Error("Bad configuration", () => ex);
                throw ex;
            }

            var deviceId = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim().Split('='))
                .Where(kv => kv.Length == 2 && kv[0].Equals("DeviceId"))
                .Select(kv => kv[1])
                .FirstOrDefault();
            if (string.IsNullOrEmpty(deviceId)) {
                var ex = new InvalidConfigurationException(
                    "Must have device connection string");
                _logger.Error("Bad configuration", () => ex);
                throw ex;
            }

            var transport = _config.Transport;

            if (connectionString.IndexOf("GatewayHostName=",
                StringComparison.InvariantCultureIgnoreCase) != -1) {
                // Running in edge context - can only use mqtt over tcp at this point
                transport = TransportOption.MqttOverTcp;
            }

            if (_config.BypassCertVerification) {
                // Running in debug mode - can only use mqtt over tcp
                transport = TransportOption.MqttOverTcp;
            }

            // Configure transports
            var transportSettings = new List<ITransportSettings>();
            if ((transport & TransportOption.Mqtt) != 0) {
                if ((transport & TransportOption.MqttOverTcp) != 0) {
                    var setting = new MqttTransportSettings(
                        TransportType.Mqtt_Tcp_Only);
                    if (_config.BypassCertVerification) {
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
            if ((transport & TransportOption.Amqp) != 0) {
                if ((transport & TransportOption.AmqpOverTcp) != 0) {
                    transportSettings.Add(new AmqpTransportSettings(
                        TransportType.Amqp_Tcp_Only));
                }
                else {
                    transportSettings.Add(new AmqpTransportSettings(
                        TransportType.Amqp_WebSocket_Only));
                }
            }
            DeviceClient client;
            if (transportSettings.Count != 0) {
                client = DeviceClient.CreateFromConnectionString(connectionString,
                    transportSettings.ToArray());
            }
            else {
                // Any
                client = DeviceClient.CreateFromConnectionString(connectionString);
            }

            // TODO: If any, retry with any_ws...:
            await client.OpenAsync();
            DeviceId = deviceId;
            return client;
        }

        /// <summary>
        /// Add certificate in local cert store for use by client for secure connection
        /// to IoT Edge runtime
        /// </summary>
        private void InstallCert(string certPath) {
            if (!File.Exists(certPath)) {
                // We cannot proceed further without a proper cert file
                _logger.Error($"Missing certificate file: {certPath}",
                    () => { });
                throw new InvalidOperationException("Missing certificate file.");
            }

            var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            store.Add(new X509Certificate2(X509Certificate.CreateFromCertFile(certPath)));
            _logger.Info("Added Cert: " + certPath, () => { });
            store.Close();
        }

        private DeviceClient _client;
        private Twin _twin;

        private readonly IMethodRouter _router;
        private readonly ISettingsRouter _settings;
        private readonly ILogger _logger;
        private readonly IHostConfig _config;
        private readonly SemaphoreSlim _lock =
            new SemaphoreSlim(1);
        private readonly Dictionary<string, dynamic> _reported =
            new Dictionary<string, dynamic>();
    }
}
