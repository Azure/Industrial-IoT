// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.Diagnostics.Default {
    using Microsoft.Extensions.Logging;
    using Prometheus;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Start and stop metric server
    /// </summary>
    public class MetricServerHost : IHostProcess, IAsyncDisposable {
        /// <summary>
        /// Auto registers metric server
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public MetricServerHost(IMetricServerConfig config, ILogger logger) {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _metricServer = new KestrelMetricServer(_config.Port);
        }

        /// <inheritdoc/>
        public ValueTask StartAsync() {
            _metricServer.Start();
            _logger.LogInformation("Started prometheus at {0}/metrics", _config.Port);
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync() {
            await _metricServer.StopAsync().ConfigureAwait(false);
            _logger.LogInformation("Metric server stopped.");
        }

        private readonly IMetricServer _metricServer;
        private readonly IMetricServerConfig _config;
        private readonly ILogger _logger;
    }
}
