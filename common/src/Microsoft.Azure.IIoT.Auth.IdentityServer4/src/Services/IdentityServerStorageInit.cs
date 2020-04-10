// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.IdentityServer4.Services {
    using Serilog;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Initialized the root resources
    /// </summary>
    public class IdentityServerStorageInit : IHostProcess {

        /// <summary>
        /// Create configuration process
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="clients"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public IdentityServerStorageInit(IResourceRepository resources,
            IClientRepository clients, IIdentityServerConfig config, ILogger logger) {
            _resources = resources ?? throw new ArgumentNullException(nameof(resources));
            _clients = clients ?? throw new ArgumentNullException(nameof(clients));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task StartAsync() {
            try {
                if (_config.Clients != null) {
                    foreach (var client in _config.Clients) {
                        await _clients.CreateOrUpdateAsync(client);
                    }
                }
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to add root user");
            }
            try {
                if (_config.Ids != null) {
                    foreach (var resource in _config.Ids) {
                        await _resources.CreateOrUpdateAsync(resource);
                    }
                }
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to add identity resources");
            }
            try {
                if (_config.Apis != null) {
                    foreach (var resource in _config.Apis) {
                        await _resources.CreateOrUpdateAsync(resource);
                    }
                }
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to add root user");
            }
        }

        /// <inheritdoc/>
        public Task StopAsync() {
            return Task.CompletedTask;
        }

        private readonly IResourceRepository _resources;
        private readonly IClientRepository _clients;
        private readonly IIdentityServerConfig _config;
        private readonly ILogger _logger;
    }
}