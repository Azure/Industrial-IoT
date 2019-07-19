// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Transport {
    using Serilog;
    using Microsoft.AspNetCore.Http;
    using Opc.Ua;
    using System;
    using System.Net;
    using System.Threading.Tasks;

    /// <summary>
    /// Decodes and forwards http based requests to the server and returns
    /// server responses to clients.
    /// </summary>
    public class HttpMiddleware {

        /// <summary>
        /// Creates middleware to forward requests to controller
        /// </summary>
        /// <param name="next"></param>
        /// <param name="encoder"></param>
        /// <param name="listener"></param>
        /// <param name="logger"></param>
        public HttpMiddleware(RequestDelegate next, IMessageSerializer encoder,
            IHttpChannelListener listener, ILogger logger) {
            _next = next;
            _encoder = encoder ??
                throw new ArgumentNullException(nameof(encoder));
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            _listener = listener ??
                throw new ArgumentNullException(nameof(listener));
        }

        /// <summary>
        /// Middleware invoke entry point which forwards to controller
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context) {
            var handled = await ProcessAsync(context);
            if (!handled) {
                await _next(context);
            }
        }

        /// <summary>
        /// Middleware invoke entry point which forwards to controller
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<bool> ProcessAsync(HttpContext context) {
            if (!context.Request.Method.Equals(HttpMethods.Post)) {
                return false;
            }
            // Decode request
            var message = _encoder.Decode(context.Request.ContentType,
                context.Request.Body);
            if (!(message is IServiceRequest request)) {
                _logger.Debug("Bad UA service request.");
                return false;
            }
            try {
                _logger.Verbose("Processing UA request...");
                var response = await _listener.ProcessAsync(context, request);
                // Encode content as per encoding requested
                context.Response.ContentType = context.Request.ContentType;
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                // context.Response.ContentLength = buffer.Length;
                using (context.Response.Body) {
                    _encoder.Encode(context.Request.ContentType,
                        context.Response.Body, response);
                }
                _logger.Verbose("Processed UA request.");
            }
            catch {
                context.Response.ContentType = context.Request.ContentType;
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
            return true;
        }

        private readonly RequestDelegate _next;
        private readonly ILogger _logger;
        private readonly IHttpChannelListener _listener;
        private readonly IMessageSerializer _encoder;
    }
}
