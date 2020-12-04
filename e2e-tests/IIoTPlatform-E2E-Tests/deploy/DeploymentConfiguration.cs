// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Deploy {
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Common.Exceptions;
    using System.Threading;
    using Microsoft.Azure.Devices;
    using TestExtensions;

    public abstract class DeploymentConfiguration : IIoTHubEdgeDeployment {

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
                        _context.OutputHelper?.WriteLine("Add new IoT Hub device configuration");
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
                        _context.OutputHelper?.WriteLine("IoT Hub device configuration already existed, remove and recreate it");
                        await _context.RegistryHelper.RegistryManager.RemoveConfigurationAsync(configuration.Id, ct);
                        var added = await _context.RegistryHelper.RegistryManager.AddConfigurationAsync(
                            configuration, ct);
                        return added;
                    }
                }

                _context.OutputHelper?.WriteLine("IoT Hub device configuration will be updated");
                // Try update existing configuration
                var result = await _context.RegistryHelper.RegistryManager.UpdateConfigurationAsync(
                    configuration, forceUpdate, ct);
                return result;
            }
            catch (Exception e) {
                _context.OutputHelper?.WriteLine("Error while creating or updating IoT Hub device configuration! {0}", e.Message);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> CreateOrUpdateLayeredDeploymentAsync(CancellationToken token) {
            var configuration = await CreateOrUpdateConfigurationAsync(new Configuration(DeploymentName) {
                Content = new ConfigurationContent { ModulesContent = CreateDeploymentModules() },
                TargetCondition = TestConstants.TargetCondition,
                Priority = Priority
            }, true, DeploymentName, token);

            return (configuration != null);
        }

        protected readonly IIoTPlatformTestContext _context;

        /// <summary>
        /// Create a deployment modules object
        /// </summary>
        protected abstract IDictionary<string, IDictionary<string, object>> CreateDeploymentModules();

        /// <summary>
        /// The desired rank of deployment
        /// </summary>
        protected abstract int Priority { get; }

        /// <summary>
        /// Identifier of deployment
        /// </summary>
        protected abstract string DeploymentName { get; }
    }
}
