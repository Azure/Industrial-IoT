// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.EdgeService.Runtime {
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Diagnostics;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Exceptions;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using System;
    using System.IO;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Main entry point
    /// </summary>
    public class EdgeService : IEdgeService {

        /// <summary>
        /// Create edge service module
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        /// <param name="router"></param>
        public EdgeService(IEdgeRequestRouter router, IEdgePropertyHandler settings,
            IEdgeConfig config, ILogger logger) {
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));

            if (!_config.BypassCertVerification) {
                InstallCert();
            }
        }

        /// <summary>
        /// Start service
        /// </summary>
        /// <returns></returns>
        public void Start() {
            if (_task != null) {
                throw new InvalidOperationException("Already running");
            }
            _running = new CancellationTokenSource();
            _task = RunAsync(_running.Token);
            // Done
        }

        /// <summary>
        /// Stop service
        /// </summary>
        public void Stop() {
            if (_task != null) {
                _running.Cancel();
                _task.Wait();
            }
            _running = null;
            _task = null;
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose() {
            Stop();
        }

        /// <summary>
        /// Run module task
        /// </summary>
        /// <returns></returns>
        public async Task RunAsync(CancellationToken ct) {

            _logger.Info($"{ServiceInfo.NAME} edge service started", () => Uptime.ProcessId);

            // Create client
            var client = await CreateDeviceClientAsync();

            var twin = client.GetTwinAsync();

            // Register callback to be called when a method request is received by the module
            await client.SetMethodDefaultHandlerAsync((request, _) => {
                return _router.InvokeMethodAsync(request);
            }, null);

            // Register callback to be called when settings change - updates settings...
            await client.SetDesiredPropertyUpdateCallbackAsync(async (settings, _) => {
                var reported = await _settings.ProcessSettings(settings);
                await client.UpdateReportedPropertiesAsync(reported);
            }, null);

            while (!ct.IsCancellationRequested) {

                /// Run
                /// // TODO
            }

            await client.CloseAsync();

            // Done
            _logger.Info($"{ServiceInfo.NAME} edge service exiting", () => Uptime.ProcessId);
        }

        /// <summary>
        /// Create and open a DeviceClient using the connection string
        /// </summary>
        /// <returns>Device client</returns>
        private async Task<DeviceClient> CreateDeviceClientAsync() {
            var mqttSetting = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);

            if (_config.BypassCertVerification) {
                mqttSetting.RemoteCertificateValidationCallback = 
                    (sender, certificate, chain, sslPolicyErrors) => true;
            }

            // The Edge runtime injects this as an environment variable
            var connectionString = _config.EdgeHubConnectionString;

            if (string.IsNullOrEmpty(connectionString) ||
                connectionString.ToLowerInvariant().Contains("your azure iot hub")) {
                var ex = new InvalidConfigurationException(
                    "The edge configuration is incomplete missing the module " +
                    "connection string for Azure IoT Edge or IoT Hub. " +
                    "You either have to run the module under the control of the " +
                    "edge hub, or manually set the 'EdgeHubConnectionString' environment" +
                    "variable or configuration value in the 'appsettings.json' file.");
                _logger.Error("Bad configuration", () => ex);
                throw ex;
            }

            var ioTHubModuleClient = DeviceClient.CreateFromConnectionString(connectionString,
                new ITransportSettings[] { mqttSetting });
            // Open a connection to the Edge runtime
            await ioTHubModuleClient.OpenAsync();
            return ioTHubModuleClient;
        }

        /// <summary>
        /// Add certificate in local cert store for use by client for secure connection 
        /// to IoT Edge runtime
        /// </summary>
        private static void InstallCert() {
            var certPath = Environment.GetEnvironmentVariable("EdgeModuleCACertificateFile");
            if (string.IsNullOrWhiteSpace(certPath)) {
                // We cannot proceed further without a proper cert file
                Console.WriteLine($"Missing path to certificate collection file: {certPath}");
                throw new InvalidOperationException("Missing path to certificate file.");
            }
            else if (!File.Exists(certPath)) {
                // We cannot proceed further without a proper cert file
                Console.WriteLine($"Missing path to certificate collection file: {certPath}");
                throw new InvalidOperationException("Missing certificate file.");
            }
            var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            store.Add(new X509Certificate2(X509Certificate.CreateFromCertFile(certPath)));
            Console.WriteLine("Added Cert: " + certPath);
            store.Close();
        }

        private readonly IEdgeRequestRouter _router;
        private readonly IEdgePropertyHandler _settings;
        private readonly ILogger _logger;
        private readonly IEdgeConfig _config;
        private CancellationTokenSource _running;
        private Task _task;
    }
}
