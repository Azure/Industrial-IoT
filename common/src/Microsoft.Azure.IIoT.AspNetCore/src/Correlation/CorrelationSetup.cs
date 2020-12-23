// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.Correlation {
    using System;
    using System.Linq;
    using Microsoft.AspNetCore.Http;
    using System.Threading.Tasks;
    using Serilog.Context;
    using Serilog;

    /// <summary>
    /// Correlation setup implementation
    /// </summary>
    public class CorrelationSetup {

        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        /// <summary>
        /// Default constructor
        /// </summary>
        public CorrelationSetup(RequestDelegate next, ILogger logger) {
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
            if (context.Response.HasStarted) {
                return;
            }
            string correlationId = null;
            var key = context.Request.Headers.Keys.FirstOrDefault(n => n.Equals("Request-Id"));
            if (!string.IsNullOrWhiteSpace(key)) {
                correlationId = context.Request.Headers[key];
                _logger.Verbose("Header contained CorrelationId: {@CorrelationId}", correlationId);
            }
            else {
                correlationId = Guid.NewGuid().ToString();
                _logger.Verbose("Generated new CorrelationId: {@CorrelationId}", correlationId);
            }
            context.Response.Headers.Append("Request-Id", correlationId);

            using (LogContext.PushProperty("XCorrelationId", correlationId)) {
                await _next.Invoke(context);
            }
        }
    }
}
