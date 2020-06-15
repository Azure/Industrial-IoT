// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Processor.Events {

    using Microsoft.Azure.IIoT.Services.Processor.Events.Runtime;
    using Microsoft.Extensions.Hosting;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Serilog;

    /// <summary>
    /// Generic host service which manages IHostProcess objects.
    /// </summary>
    class StarterService : IHostedService {

        /// <summary>
        /// List of IHostProcess objects that will be managed by this instance, provided by DI.
        /// </summary>
        private readonly List<IHostProcess> _hostProcesses;
        private readonly ILogger _logger;

        /// <summary>
        /// Details of application hosting environment.
        /// </summary>
        public IHostEnvironment HostEnvironment { get; }

        /// <summary>
        /// Handler for subscribing to application lifetime events.
        /// </summary>
        public IHostApplicationLifetime HostApplicationLifetime { get; }

        /// <summary>
        /// Runtime configuration, will be provided by DI.
        /// </summary>
        public Config Config { get; }

        /// <summary>
        /// Service information, will be provided by DI.
        /// </summary>
        public ServiceInfo ServiceInfo { get; }

        public StarterService(
            IHostEnvironment hostEnvironment,
            IHostApplicationLifetime hostApplicationLifetime,
            Config config,
            ServiceInfo serviceInfo,
            IEnumerable<IHostProcess> hostProcesses,
            ILogger logger
        ) {
            HostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
            HostApplicationLifetime = hostApplicationLifetime ?? throw new ArgumentNullException(nameof(hostApplicationLifetime));
            Config = config ?? throw new ArgumentNullException(nameof(config));
            ServiceInfo = serviceInfo ?? throw new ArgumentNullException(nameof(serviceInfo));
            _hostProcesses = hostProcesses?.ToList() ?? throw new ArgumentNullException(nameof(hostProcesses));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task StartAsync(CancellationToken cancellationToken) {
            try {
                _logger.Debug("Starting all hosts...");
                await Task.WhenAll(_hostProcesses.Select(h => h.StartAsync()));
                _logger.Information("All hosts started.");
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to start some hosts.");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task StopAsync(CancellationToken cancellationToken) {
            try {
                _logger.Debug("Stopping all hosts...");
                await Task.WhenAll(_hostProcesses.Select(h => h.StopAsync()));
                _logger.Information("All hosts stopped.");
            }
            catch (Exception ex) {
                _logger.Warning(ex, "Failed to stop all hosts.");
                throw;
            }
        }
    }
}
