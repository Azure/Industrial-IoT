// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client {
    using Microsoft.Extensions.Logging;
    using Prometheus;
    using System.Net;

    /// <summary>
    /// Metric Server extensions
    /// </summary>
    public static class MetricServerEx {

        /// <summary>
        /// Start metric server
        /// </summary>
        /// <returns></returns>
        public static void StartWhenEnabled(this IMetricServer server, IModuleConfig config, ILogger logger) {
            if (config.EnableMetrics) {
                try {
                    server.Start();
                    logger.LogInformation("Prometheus metric server started.");
                }
                catch (HttpListenerException e) {
                    logger.LogError(e, "Unable to start metric server. For more info, please check troubleshooting guide for edge metrics collection");
                }

            }
            else {
                logger.LogInformation("Metrics Collection is disabled. Not starting prometheus metric server.");
            }

        }

        /// <summary>
        /// Stop metric server if enabled
        /// </summary>
        /// <returns></returns>
        public static void StopWhenEnabled(this IMetricServer server, IModuleConfig config, ILogger logger) {
            if (config.EnableMetrics) {
                server.Stop(); ;
                logger.LogInformation("Stopped prometheus metric server");
            }
        }
    }
}
