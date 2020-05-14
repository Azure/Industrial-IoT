// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.Monitoring
{
    using Microsoft.AspNetCore.Http;
    using System.Threading.Tasks;
    using Serilog;
    using System.Diagnostics;
    using Prometheus;

    /// <summary>
    /// Response time implementation
    /// </summary>
    public class ResponseTimeMiddleware
    {

        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        /// <summary>
        /// Default constructor
        /// </summary>
        public ResponseTimeMiddleware(RequestDelegate next, ILogger logger) {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Invoke
        /// </summary>
        /// <param name="context"></param>
#pragma warning disable IDE1006 // Naming Styles
        public async Task Invoke(HttpContext context) {
#pragma warning restore IDE1006 // Naming Styles
            var sw = Stopwatch.StartNew();
            await _next(context);
            sw.Stop();

            var histogram =
                Metrics
                    .CreateHistogram(
                        "iiot_service_api_rt_seconds",
                        "API Response Time in seconds",
                        "method",
                        "path");

            histogram
                .WithLabels(context.Request.Method, context.Request.Path)
                .Observe(sw.Elapsed.TotalSeconds);
        }
    }
}
