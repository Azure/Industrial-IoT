// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests {

    using IIoTPlatform_E2E_Tests.Config;
    using Microsoft.Azure.Devices;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using TestExtensions;

    /// <summary>
    /// Helper for managing IoT Hub device registry.
    /// </summary>
    public class RegistryHelper {

        public RegistryHelper(IIoTHubConfig ioTHubConfig) {
            _ioTHubConfig = ioTHubConfig ?? throw new ArgumentNullException(nameof(ioTHubConfig));

            _registryManager = RegistryManager.CreateFromConnectionString(_ioTHubConfig.IoTHubConnectionString);
        }

        /// <summary>
        /// Wait until IIoT modules are in Connected state.
        /// </summary>
        /// <param name="deviceId"> IoT Edge device id </param>
        /// <param name="ct"> Cancellation token </param>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="moduleNames"> List of modules to wait for, defaults to ModuleNamesDefault if not specified </param>
        /// <returns></returns>
        public async Task WaitForIIoTModulesConnectedAsync(
            string deviceId,
            CancellationToken ct,
            IIoTPlatformTestContext context,
            IEnumerable<string> moduleNames = null
        ) {
            moduleNames ??= ModuleNamesDefault;

            try {
                while (true) {
                    var modules = await _registryManager.GetModulesOnDeviceAsync(deviceId, ct);
                    var connectedModulesCout = modules
                        .Where(m => moduleNames.Contains(m.Id))
                        .Where(m => m.ConnectionState == DeviceConnectionState.Connected)
                        .Count();

                    if (connectedModulesCout == moduleNames.Count()) {
                        context.OutputHelper?.WriteLine("All required IoT Edge modules are loaded!");
                        return;
                    }

                    await Task.Delay(TestConstants.DefaultDelayMilliseconds, ct);
                }
            }
            catch (OperationCanceledException) {
                context.OutputHelper?.WriteLine("Waiting for IoT Edge modules to be loaded timeout - please check iot edge device for details");
                throw;
            }
            catch (Exception e) {
                context.OutputHelper?.WriteLine("Error occurred while waiting for edge Modules");
                context.OutputHelper?.WriteLine(e.Message);
                throw;
            }
        }

        /// <summary>
        /// Access To Registry Manager
        /// </summary>
        public RegistryManager RegistryManager => _registryManager;

        /// <summary>
        /// Default value fo IIoT module names.
        /// </summary>
        public static IEnumerable<string> ModuleNamesDefault = new string[] { "publisher", "twin", "discovery" };

        private readonly IIoTHubConfig _ioTHubConfig;
        private readonly RegistryManager _registryManager;
    }
}
