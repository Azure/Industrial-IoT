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

            try {
                while (true) {
                    var modules = await RegistryManager.GetModulesOnDeviceAsync(deviceId, ct);
                    var connectedModulesCout = modules
                        .Where(m => moduleNames.Contains(m.Id))
                        .Where(m => m.ConnectionState == DeviceConnectionState.Connected)
                        .Count();

                    if (connectedModulesCout == moduleNames.Count()) {
                        _context.OutputHelper?.WriteLine("All required IoT Edge modules are loaded!");
                        return;
                    }

                    await Task.Delay(TestConstants.DefaultDelayMilliseconds, ct);
                }
            }
            catch (OperationCanceledException) {
                _context.OutputHelper?.WriteLine("Waiting for IoT Edge modules to be loaded timeout - please check iot edge device for details");
                throw;
            }
            catch (Exception e) {
                _context.OutputHelper?.WriteLine("Error occurred while waiting for edge Modules");
                _context.OutputHelper?.WriteLine(e.Message);
                throw;
            }
        }

        /// <summary>
        /// Create or update deployment configuration
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="forceUpdate"></param>
        /// <param name="deploymentId"></param>
        /// <param name="ct"> Cancellation token </param>
        public async Task<Configuration> CreateOrUpdateConfigurationAsync(
            Configuration configuration,
            bool forceUpdate,
            string deploymentId,
            CancellationToken ct = default
        ) {
            try {
                var getConfig = await RegistryManager.GetConfigurationAsync(deploymentId, ct);
                if (getConfig == null) {
                    // First try create configuration
                    try {
                        _context.OutputHelper?.WriteLine("Add new IoT Hub device configuration");
                        var added = await RegistryManager.AddConfigurationAsync(
                        configuration, ct);
                        return added;
                    }
                    catch (DeviceAlreadyExistsException) when (forceUpdate) {
                        //
                        // Technically update below should now work but for
                        // some reason it does not.
                        // Remove and re-add in case we are forcing updates.
                        //
                        _context.OutputHelper?.WriteLine("IoT Hub device configuration already existed, remove and recreate it");
                        await RegistryManager.RemoveConfigurationAsync(configuration.Id, ct);
                        var added = await RegistryManager.AddConfigurationAsync(
                            configuration, ct);
                        return added;
                    }
                }

                _context.OutputHelper?.WriteLine("IoT Hub device configuration will be updated");
                // Try update existing configuration
                var result = await RegistryManager.UpdateConfigurationAsync(
                    configuration, forceUpdate, ct);
                return result;
            }
            catch (Exception e) {
                _context.OutputHelper?.WriteLine("Error while creating or updating IoT Hub device configuration! {0}", e.Message);
                throw;
            }
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
