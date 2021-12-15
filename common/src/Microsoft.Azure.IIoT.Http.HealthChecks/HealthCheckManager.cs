// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http.HealthChecks {
    using Serilog;
    using System;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <inheritdoc/>
    public class HealthCheckManager : IHealthCheckManager {
        private const string kLivenessSuffix = "healthz";
        private const string kReadinessSuffix = "ready";
        private readonly ILogger _logger;
        private bool _isInitialized;
        private HttpListener _httpListener;
        private CancellationTokenSource _cancellationTokenSource;

        /// <inheritdoc/>
        public bool IsLive { get; set; } = true;

        /// <inheritdoc/>
        public bool IsReady { get; set; }

        /// <summary>
        /// Constructor for health checks.
        /// </summary>
        public HealthCheckManager(ILogger logger) {
            _logger = logger;
        }

        /// <inheritdoc/>
        public void Start(uint port) {
            if (_isInitialized) {
                _logger.Warning("Health checks already initialized.");
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();

            try {
                // Create HTTP listener for probes.
                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add($"http://localhost:{port}/{kLivenessSuffix}/");
                _httpListener.Prefixes.Add($"http://localhost:{port}/{kReadinessSuffix}/");
                _httpListener.Start();
            }
            catch (Exception ex) {
                _logger.Error(ex, "Cannot initialize health checks.");
                return;
            }

            // Start processing HTTP requests.
            Run();

            _isInitialized = true;
            _logger.Information("Health checks started.");
        }

        /// <inheritdoc/>
        public void Stop() {
            if (!_isInitialized) {
                _logger.Warning("Health checks not initialized.");
                return;
            }

            _cancellationTokenSource.Cancel();
            _httpListener.Stop();
            _isInitialized = false;
            _logger.Information("Health checks stopped.");
        }

        /// <summary>
        /// Process HTTP requests.
        /// </summary>
        private async void Run() {
            while (!_cancellationTokenSource.Token.IsCancellationRequested) {
                try {
                    // Wait for a request to process. It should be cancelled if the HTTP listener is stopped.
                    var httpListenerContext = await _httpListener.GetContextAsync();
                    var rawUrl = httpListenerContext.Request.RawUrl.Trim('/');
                    if ((string.Equals(rawUrl, kLivenessSuffix, StringComparison.OrdinalIgnoreCase) && IsLive) ||
                        (string.Equals(rawUrl, kReadinessSuffix, StringComparison.OrdinalIgnoreCase) && IsReady)) {
                        await WriteResponse(httpListenerContext.Response, "OK", HttpStatusCode.OK);
                    }
                    else {
                        httpListenerContext.Response.Abort();
                    }
                }
                catch {
                    // Wait for a bit in-case it was a transient error.
                    await Task.Delay(1000);
                }
            }
        }

        /// <summary>
        /// Write HTTP response.
        /// </summary>
        /// <param name="httpListenerResponse">Response object to write to.</param>
        /// <param name="text">Text to return in the response.</param>
        /// <param name="httpStatusCode">Status code to return in the response.</param>
        private static async Task WriteResponse(HttpListenerResponse httpListenerResponse, string text, HttpStatusCode httpStatusCode) {
            var data = Encoding.UTF8.GetBytes(text);
            httpListenerResponse.ContentType = "text/plain";
            httpListenerResponse.ContentEncoding = Encoding.UTF8;
            httpListenerResponse.ContentLength64 = data.LongLength;
            httpListenerResponse.StatusCode = (int)httpStatusCode;

            // Write and close output stream.
            await httpListenerResponse.OutputStream.WriteAsync(data);
            httpListenerResponse.Close();
        }
    }
}
