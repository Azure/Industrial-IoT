// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Utils {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Autofac;
    using Serilog;

    /// <summary>
    /// Host auto starter
    /// </summary>
    public class HostAutoStart : IDisposable, IStartable {

        /// <summary>
        /// Create host auto starter
        /// </summary>
        /// <param name="hosts"></param>
        /// <param name="logger"></param>
        public HostAutoStart(IEnumerable<IHostProcess> hosts, ILogger logger) {
            _host = hosts?.ToList() ?? throw new ArgumentNullException(nameof(hosts));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public void Start() {
            try {
                _logger.Debug("Starting all hosts...");
                Task.WhenAll(_host.Select(h => h.StartAsync())).Wait();
                _logger.Information("All hosts started.");
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to start some hosts.");
                throw ex;
            }
        }

        /// <inheritdoc/>
        public void Dispose() {
            try {
                _logger.Debug("Stopping all hosts...");
                Task.WhenAll(_host.Select(h => h.StopAsync())).Wait();
                _logger.Information("All hosts stopped.");
            }
            catch (Exception ex) {
                _logger.Warning(ex, "Failed to stop all hosts.");
            }
        }

        private readonly List<IHostProcess> _host;
        private readonly ILogger _logger;
    }
}