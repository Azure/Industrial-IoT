// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging.Default {

    using Serilog;
    using System;
    using System.Threading.Tasks;
    using Prometheus;

    /// <summary>
    /// Start and stop metric server
    /// </summary>
    public class MetricServerHost : IHostProcess {

        /// <summary>
        /// Auto registers metric server
        /// </summary>
        /// <param name="port"></param>
        /// <param name="logger"></param>
        public MetricServerHost(int? port, ILogger logger) {
            _port = port ?? throw new ArgumentNullException(nameof(port));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _metricServer = new KestrelMetricServer(_port);
        }

        /// <inheritdoc/>
        public Task StartAsync() {
            _metricServer.Start();
            _logger.Information("Started prometheus at {0}/metrics", _port);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task StopAsync() {
            await _metricServer.StopAsync();
            _logger.Information("Metric server stopped.");
        }

        private readonly IMetricServer _metricServer;
        private readonly int _port;
        private readonly ILogger _logger;
    }
}