// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Client {
    using Microsoft.Azure.Devices;
    using Microsoft.Azure.Devices.Common.Exceptions;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implementation of configuration services using service sdk.
    /// </summary>
    public sealed class IoTHubConfigurationClient : IIoTHubConfigurationServices {

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public IoTHubConfigurationClient(IIoTHubConfig config, ILogger logger) {
            if (string.IsNullOrEmpty(config?.IoTHubConnString)) {
                throw new ArgumentNullException(nameof(config.IoTHubConnString));
            }

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _registry = RegistryManager.CreateFromConnectionString(config.IoTHubConnString);
            _registry.OpenAsync().Wait();
        }


        /// <inheritdoc/>
        public async Task ApplyConfigurationAsync(string deviceId,
            ConfigurationContentModel configuration, CancellationToken ct) {
            try {
                await _registry.ApplyConfigurationContentOnDeviceAsync(deviceId,
                    configuration.ToContent(), ct);
            }
            catch (Exception e) {
                _logger.Verbose(e, "Apply configuration failed ");
                throw e.Rethrow();
            }
        }

        /// <inheritdoc/>
        public async Task<ConfigurationModel> CreateOrUpdateConfigurationAsync(
            ConfigurationModel configuration, bool forceUpdate, CancellationToken ct) {

            try {
                if (string.IsNullOrEmpty(configuration.Etag)) {
                    // First try create configuration
                    try {
                        var added = await _registry.AddConfigurationAsync(
                            configuration.ToConfiguration(), ct);
                        return added.ToModel();
                    }
                    catch (DeviceAlreadyExistsException) when (forceUpdate) {
                        //
                        // Technically update below should now work but for
                        // some reason it does not.
                        // Remove and re-add in case we are forcing updates.
                        //
                        await _registry.RemoveConfigurationAsync(configuration.Id, ct);
                        var added = await _registry.AddConfigurationAsync(
                            configuration.ToConfiguration(), ct);
                        return added.ToModel();
                    }
                }

                // Try update existing configuration
                var result = await _registry.UpdateConfigurationAsync(
                    configuration.ToConfiguration(), forceUpdate, ct);
                return result.ToModel();
            }
            catch (Exception e) {
                _logger.Verbose(e,
                    "Update configuration failed in CreateOrUpdate");
                throw e.Rethrow();
            }
        }

        /// <inheritdoc/>
        public async Task<ConfigurationModel> GetConfigurationAsync(
            string configurationId, CancellationToken ct) {
            try {
                var configuration = await _registry.GetConfigurationAsync(
                    configurationId, ct);
                return configuration.ToModel();
            }
            catch (Exception e) {
                _logger.Verbose(e, "Get configuration failed");
                throw e.Rethrow();
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ConfigurationModel>> ListConfigurationsAsync(
            int? maxCount, CancellationToken ct) {
            try {
                var configurations = await _registry.GetConfigurationsAsync(
                    maxCount ?? int.MaxValue, ct);
                return configurations.Select(c => c.ToModel());
            }
            catch (Exception e) {
                _logger.Verbose(e, "List configurations failed");
                throw e.Rethrow();
            }
        }

        /// <inheritdoc/>
        public async Task DeleteConfigurationAsync(string configurationId,
            string etag, CancellationToken ct) {
            try {
                if (string.IsNullOrEmpty(etag)) {
                    await _registry.RemoveConfigurationAsync(configurationId, ct);
                }
                else {
                    await _registry.RemoveConfigurationAsync(
                        new Configuration(configurationId) { ETag = etag }, ct);
                }
            }
            catch (Exception e) {
                _logger.Verbose(e, "Delete configuration failed");
                throw e.Rethrow();
            }
        }

        private readonly RegistryManager _registry;
        private readonly ILogger _logger;
    }
}
