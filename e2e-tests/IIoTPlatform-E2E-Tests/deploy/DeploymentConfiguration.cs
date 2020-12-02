// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.TestExtensions {
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Common.Exceptions;
    using System.Threading;
    using Microsoft.Azure.Devices;

    public class DeploymentConfiguration {

        public DeploymentConfiguration(IIoTPlatformTestContext context) {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Create or update deployment configuration
        /// </summary>
        /// <returns>configuration</returns>
        /// <returns>forceUpdate</returns>
        /// <returns>deploymentId</returns>
        /// <returns>ct</returns>
        public async Task<Configuration> CreateOrUpdateConfigurationAsync(
            Configuration configuration, bool forceUpdate, string deploymentId, CancellationToken ct) {
            try {
                var getConfig = await _context.RegistryHelper.RegistryManager.GetConfigurationAsync(deploymentId, ct);
                if (getConfig == null) {
                    // First try create configuration
                    try {
                        var added = await _context.RegistryHelper.RegistryManager.AddConfigurationAsync(
                        configuration, ct);
                        return added;
                    }
                    catch (DeviceAlreadyExistsException) when (forceUpdate) {
                        //
                        // Technically update below should now work but for
                        // some reason it does not.
                        // Remove and re-add in case we are forcing updates.
                        //
                        await _context.RegistryHelper.RegistryManager.RemoveConfigurationAsync(configuration.Id, ct);
                        var added = await _context.RegistryHelper.RegistryManager.AddConfigurationAsync(
                            configuration, ct);
                        return added;
                    }
                }

                // Try update existing configuration
                var result = await _context.RegistryHelper.RegistryManager.UpdateConfigurationAsync(
                    configuration, forceUpdate, ct);
                return result;
            }
            catch (Exception e) {
                throw e;
            }
        }

        private readonly IIoTPlatformTestContext _context;
    }
}
