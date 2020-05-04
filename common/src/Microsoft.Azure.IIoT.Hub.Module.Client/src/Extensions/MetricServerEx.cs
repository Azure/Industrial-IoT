// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client {
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
        public static void StartWhenEnabled(this IMetricServer server, IModuleConfig config, Serilog.ILogger logger) {
            if (config.EnableMetrics) {
                try {
                    server.Start();
                    logger.Information("Prometheus metric server started.");
                }
                catch (HttpListenerException e){
                    logger.Error(e, "Unable to start metric server. To enable edge metrics collection, please run the following commands in an elevated window:\n" +
                        "\nnetsh http add urlacl url=http://+:9700/metrics user=Everyone" +
                        "\nnetsh http add urlacl url=http://+:9701/metrics user=Everyone" +
                        "\nnetsh http add urlacl url=http://+:9702/metrics user=Everyone\n");
                }

            } 
            else {
                logger.Information("Metrics Collection is disabled. Not starting prometheus metric server.");
            }

        }

        /// <summary>
        /// Stop metric server if enabled
        /// </summary>
        /// <returns></returns>
        public static void StopWhenEnabled(this MetricServer server, IModuleConfig config, Serilog.ILogger logger) {
            if (config.EnableMetrics) {
                server.Stop(); ;
                logger.Information("Stopped prometheus metric server");
            }
        }
    }
}
