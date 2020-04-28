// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client {
    using Prometheus;

    /// <summary>
    /// Metric Server extensions
    /// </summary>
    public static class MetricServerEx {

        /// <summary>
        /// Start metric server
        /// </summary>
        /// <returns></returns>
        public static void StartWhenEnabled(this MetricServer server, IModuleConfig config, Serilog.ILogger logger) {
            if (config.EnableMetrics) {
                server.Start();
                logger.Information("Started prometheus server");
            } 
            else {
                logger.Information("Metrics Collection is disabled. Not starting prometheus server.");
            }

        }

        /// <summary>
        /// Stop metric server if enabled
        /// </summary>
        /// <returns></returns>
        public static void StopWhenEnabled(this MetricServer server, IModuleConfig config, Serilog.ILogger logger) {
            if (config.EnableMetrics) {
                server.Stop(); ;
                logger.Information("Stopped prometheus server");
            }
        }
    }
}
