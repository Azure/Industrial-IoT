// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisher_AE_E2E_Tests {

    using OpcPublisher_AE_E2E_Tests.TestExtensions;
    using Microsoft.Azure.Devices;
    using Microsoft.Azure.Devices.Common.Exceptions;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Helper for managing IoT Hub device registry.
    /// </summary>
    public class RegistryHelper : IDisposable {

        /// <summary>
        /// Constructor of RegistryHelper class.
        /// </summary>
        /// <param name="context"> Shared context for E2E tests </param>
        public RegistryHelper(IIoTPlatformTestContext context) {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            RegistryManager = RegistryManager.CreateFromConnectionString(context.IoTHubConfig.IoTHubConnectionString);
        }

        /// <summary>
        /// Wait until IIoT modules are in Connected state.
        /// </summary>
        /// <param name="deviceId"> IoT Edge device id </param>
        /// <param name="ct"> Cancellation token </param>
        /// <param name="moduleNames"> List of modules to wait for, defaults to ModuleNamesDefault if not specified </param>
        /// <returns></returns>
        public async Task WaitForIIoTModulesConnectedAsync(
            string deviceId,
            CancellationToken ct,
            IEnumerable<string> moduleNames = null
        ) {
            moduleNames ??= ModuleNamesDefault;
            var sw = Stopwatch.StartNew();
            try {
                while (true) {
                    var modules = await RegistryManager.GetModulesOnDeviceAsync(deviceId, ct).ConfigureAwait(false);
                    var connectedModulesCout = modules
                        .Where(m => moduleNames.Contains(m.Id))
                        .Where(m => m.ConnectionState == DeviceConnectionState.Connected)
                        .Count();

                    if (connectedModulesCout == moduleNames.Count()) {
                        _context.OutputHelper?.WriteLine($"All required IoT Edge modules are connected! (took {sw.Elapsed})");
                        return;
                    }

                    await Task.Delay(TestConstants.DefaultDelayMilliseconds, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) {
                _context.OutputHelper?.WriteLine($"Waiting for IoT Edge modules to be loaded timeout timeout after {sw.Elapsed} - please check iot edge device for details");
                throw;
            }
            catch (Exception e) {
                _context.OutputHelper?.WriteLine($"Error {e.Message} occurred while waiting for edge Modules after {sw.Elapsed}");
                throw;
            }
        }

        /// <summary>
        /// Wait until one successful deployment is reported.
        /// </summary>
        public async Task WaitForSuccessfulDeploymentAsync(
            Configuration deploymentConfiguration,
            CancellationToken ct) {
            var sw = Stopwatch.StartNew();
            try {
                while (true) {
                    ct.ThrowIfCancellationRequested();

                    var activeConfiguration = await RegistryManager
                        .GetConfigurationAsync(deploymentConfiguration.Id, ct)
                        .ConfigureAwait(false);

                    if (activeConfiguration != null
                        && Equals(activeConfiguration, deploymentConfiguration)
                        && activeConfiguration.SystemMetrics.Results.ContainsKey("reportedSuccessfulCount")
                        && activeConfiguration.SystemMetrics.Results["reportedSuccessfulCount"] == 1) {
                        _context.OutputHelper?.WriteLine($"All required IoT Edge modules are deployed! (took {sw.Elapsed})");
                        return;
                    }

                    await Task.Delay(TestConstants.DefaultDelayMilliseconds, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) {
                _context.OutputHelper?.WriteLine($"Waiting for IoT Edge modules to be loaded timeout after {sw.Elapsed} - please check iot edge device for details");
                throw;
            }
            catch (Exception e) {
                _context.OutputHelper?.WriteLine($"Error {e.Message} occurred while waiting for edge Modules after {sw.Elapsed}");
                throw;
            }
        }

        /// <summary>
        /// Create or update deployment configuration
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="deploymentId"></param>
        /// <param name="ct"> Cancellation token </param>
        public async Task<Configuration> CreateOrUpdateConfigurationAsync(
            Configuration configuration,
            CancellationToken ct = default
        ) {
            try {
                var getConfig = await RegistryManager.GetConfigurationAsync(configuration.Id, ct).ConfigureAwait(false);

                if (getConfig == null) {
                    // First try create configuration
                    try {
                        _context.OutputHelper?.WriteLine("Add new IoT Hub device configuration");
                        return await RegistryManager.AddConfigurationAsync(configuration, ct).ConfigureAwait(false);
                    }
                    catch (DeviceAlreadyExistsException) {
                        // Technically update below should now work but for some reason it does not.
                        // Remove and re-add in case we are forcing updates.
                        _context.OutputHelper?.WriteLine("IoT Hub device configuration already existed, remove and recreate it");
                        await RegistryManager.RemoveConfigurationAsync(configuration.Id, ct).ConfigureAwait(false);
                        return await RegistryManager.AddConfigurationAsync(configuration, ct).ConfigureAwait(false);
                    }
                }

                if (Equals(configuration, getConfig)) {
                    return getConfig;
                }

                _context.OutputHelper?.WriteLine("Existing IoT Hub device configuration is different, remove and recreate it");
                await RegistryManager.RemoveConfigurationAsync(configuration.Id, ct).ConfigureAwait(false);
                return await RegistryManager.AddConfigurationAsync(configuration, ct).ConfigureAwait(false);
            }
            catch (Exception e) {
                _context.OutputHelper?.WriteLine("Error while creating or updating IoT Hub device configuration! {0}", e.Message);
                throw;
            }
        }

        /// <summary>
        /// Check equality of two deployment configurations.
        /// </summary>
        public static bool Equals(Configuration c0, Configuration c1) {
            if (c0.Id != c1.Id) {
                return false;
            }

            if (c0.TargetCondition != c1.TargetCondition) {
                return false;
            }

            if (c0.Priority != c1.Priority) {
                return false;
            }

            var c0ModulesContentCount = c0.Content?.ModulesContent?.Count ?? 0;
            var c1ModulesContentCount = c1.Content?.ModulesContent?.Count ?? 0;

            if (c0ModulesContentCount == 0 && c1ModulesContentCount == 0) {
                return true;
            }
            else if (c0ModulesContentCount != c1ModulesContentCount) {
                return false;
            }

            // After the previous checks we know that both have the same non-zero number of module contents.
            foreach (var moduleName in c1.Content.ModulesContent.Keys) {
                if (c0.Content.ModulesContent.ContainsKey(moduleName)) {
                    var moduleContent0 = c0.Content.ModulesContent[moduleName];
                    var moduleContent1 = c1.Content.ModulesContent[moduleName];

                    var diffCount = moduleContent0
                        .Count(entry => moduleContent1[entry.Key].ToString() != entry.Value.ToString());

                    if (diffCount > 0) {
                        return false;
                    }
                }
                else {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc />
        public void Dispose() {
            RegistryManager.Dispose();
        }

        /// <summary>
        /// Access To Registry Manager
        /// </summary>
        public RegistryManager RegistryManager { get; }

        /// <summary>
        /// Default value fo IIoT module names.
        /// </summary>
        public static IEnumerable<string> ModuleNamesDefault = new string[] { "publisher", "twin", "discovery" };

        private readonly IIoTPlatformTestContext _context;
    }
}
