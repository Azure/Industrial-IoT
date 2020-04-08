// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.IdentityServer4.Services {
    using Microsoft.Azure.IIoT.Auth.IdentityServer4.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.AspNetCore.Identity;
    using Serilog;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Configures the root user in the user database
    /// </summary>
    public class UserManagerStorageInit : IHostProcess {

        /// <summary>
        /// Create configuration process
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public UserManagerStorageInit(UserManager<UserModel> manager,
            IRootUserConfig config, ILogger logger) {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task StartAsync() {
            if (string.IsNullOrEmpty(_config.UserName)) {
                _logger.Debug("Skipping root user configuration.");
                return;
            }
            var rootUser = new UserModel {
                Id = _config.UserName
            };
            try {
                var exists = await _manager.FindByIdAsync(_config.UserName);
                if (exists != null) {
                    return;
                }

                if (string.IsNullOrEmpty(_config.Password)) {
                    await _manager.CreateAsync(rootUser);
                }
                else {
                    await _manager.CreateAsync(rootUser, _config.Password);
                }
                _logger.Information("Root user {user} added", _config.UserName);
            }
            catch (ConflictingResourceException) { }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to add root user");
            }
        }

        /// <inheritdoc/>
        public Task StopAsync() {
            return Task.CompletedTask;
        }

        private readonly UserManager<UserModel> _manager;
        private readonly IRootUserConfig _config;
        private readonly ILogger _logger;
    }
}