// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.Diagnostic.v2.Supervisor {
    using Microsoft.Azure.IIoT.Modules.Diagnostic.v2.Filters;
    using Microsoft.Azure.IIoT.Modules.Diagnostic.Services;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Serilog;
    using System;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Diagnostic methods controller
    /// </summary>
    [Version(1)]
    [Version(2)]
    [ExceptionsFilter]
    public class DiagnosticMethodsController : IMethodController {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="publisher"></param>
        /// <param name="logger"></param>
        public DiagnosticMethodsController(ITelemetrySender publisher, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        }

        /// <summary>
        /// Handle ping
        /// </summary>
        /// <returns></returns>
        public Task<DateTime> PingAsync(DateTime start) {
            var now = DateTime.UtcNow;
            _logger.Verbose("Processed PING: request timing: {timing}", now - start);
            return Task.FromResult(now);
        }

        /// <summary>
        /// Handle echo
        /// </summary>
        /// <returns></returns>
        public Task<JToken> EchoAsync(JToken token) {
            _logger.Verbose("Processed ECHO: {token}", token.ToString(Formatting.None));
            return Task.FromResult(token);
        }

        /// <summary>
        /// Start to publish
        /// </summary>
        /// <returns></returns>
        public async Task StartPublishAsync(int delay) {
            _publisher.Interval = TimeSpan.FromSeconds(delay);
            await _publisher.StartAsync();
        }

        /// <summary>
        /// Stop
        /// </summary>
        /// <returns></returns>
        public async Task StopPublishAsync() {
            await _publisher.StopAsync();
        }

        private readonly ILogger _logger;
        private readonly ITelemetrySender _publisher;
    }
}
