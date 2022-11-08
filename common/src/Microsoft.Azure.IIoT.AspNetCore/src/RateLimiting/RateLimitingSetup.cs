// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.RateLimiting {
    using Microsoft.AspNetCore.Http;
    using Prometheus;
    using Serilog;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Simple concurrent rate limiting setup
    /// </summary>
    public class RateLimitingSetup {

        private readonly RequestDelegate _next;
        private readonly ILogger _logger;
        private readonly int _maxConcurrentCalls;
        private readonly string _pathException;
        private volatile int _concurrentCalls;
        private volatile int _throttling;

        /// <summary>
        /// Default constructor
        /// </summary>
        public RateLimitingSetup(RequestDelegate next, IRateLimitingConfig config, ILogger logger) {
            _next = next;
            _logger = logger;
            _maxConcurrentCalls = config.AspNetCoreRateLimitingMaxConcurrentRequests;
            _pathException = config.AspNetCoreRateLimitingPathException;
            _logger.Information("Limiting requests to max {concurrentCount} concurrent requests",
                _maxConcurrentCalls);
        }

        /// <summary>
        /// Invoke
        /// </summary>
        /// <param name="context"></param>
        public async Task InvokeAsync(HttpContext context) {
            if (context.Response.HasStarted) {
                return;
            }
            if (_maxConcurrentCalls <= 0 || (_pathException != null && context.Request.Path == _pathException)) {
                await _next.Invoke(context);
            }
            else {
                try {
                    if (Interlocked.Increment(ref _concurrentCalls) > _maxConcurrentCalls) {
                        if (0 == Interlocked.CompareExchange(ref _throttling, 1, 0)) {
                            _logger.Information("Start throttling requests due to too many concurrent calls.");
                        }
                        kRateLimitedCalls.Inc();
                        context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                    }
                    else {
                        if (1 == Interlocked.Exchange(ref _throttling, 0)) {
                            _logger.Information("Stop throttling requests.");
                        }
                        await _next.Invoke(context);
                    }
                }
                finally {
                    Interlocked.Decrement(ref _concurrentCalls);
                }
            }
        }

        private static readonly Gauge kRateLimitedCalls = Metrics.CreateGauge(
              "iiot_number_rate_limited_requests",
              "Number or rate limited requests");
    }
}
