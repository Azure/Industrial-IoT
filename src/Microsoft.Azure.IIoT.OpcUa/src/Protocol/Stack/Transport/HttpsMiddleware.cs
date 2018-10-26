// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Transport {
    using Opc.Ua;
    using Microsoft.AspNetCore.Http;
    using System;
    using System.Net;
    using System.Threading.Tasks;

    /// <summary>
    /// Forwards to multiple listeners
    /// </summary>
    public class HttpsMiddleware {

        /// <summary>
        /// Creates middleware to forward requests to controller
        /// </summary>
        /// <param name="next"></param>
        /// <param name="encoder"></param>
        /// <param name="callback"></param>
        public HttpsMiddleware(RequestDelegate next, IMessageSerializer encoder,
            ITransportListenerCallback callback) {
            _next = next;
            _encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
            _listenerId = Guid.NewGuid().ToString();
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
                return false;
            }

            if (request.RequestHeader == null) {
                request.RequestHeader = new RequestHeader();
            }

            // extract token from header for sessionless messages.
            if (NodeId.IsNull(request.RequestHeader.AuthenticationToken) &&
                request.TypeId != DataTypeIds.CreateSessionRequest &&
                context.Request.Headers.TryGetValue("Authorization",
                    out var values)) {
                foreach (var value in values) {
                    if (value.StartsWith("Bearer ", StringComparison.Ordinal)) {
                        request.RequestHeader.AuthenticationToken =
                            new NodeId(value.Substring("Bearer ".Length).Trim());
                    }
                }
            }

            // Forward to listener / server
            var response = await Task.Factory.FromAsync(
                (callback, state) => _callback.BeginProcessRequest(_listenerId,
                    GetEndpointFromContext(context), request, callback, state),
                _callback.EndProcessRequest, TaskCreationOptions.DenyChildAttach);

            // Encode content as per encoding requested
            context.Response.ContentType = context.Request.ContentType;
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            // context.Response.ContentLength = buffer.Length;
            _encoder.Encode(context.Request.ContentType, context.Response.Body,
                response);
            return true;
        }

        /// <summary>
        /// Build the endpoint information from context and configuration.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private static EndpointDescription GetEndpointFromContext(
            HttpContext context) {

            // Select security policy contained in header, or reject
            var policy = SecurityPolicies.Https;
            if (context.Request.Headers.TryGetValue("OPCUA-SecurityPolicy",
                out var header)) {
                policy = header;
            }

            // Build endpoint url
            var scheme = context.Request.Scheme;
            var url = new UriBuilder {
                Scheme = scheme,
                Host = context.Request.Host.Value ?? Utils.GetHostName(),
                Port = context.Request.Host.Port ??
                    (scheme.EqualsIgnoreCase(Utils.UriSchemeHttps) ?
                        443 : 80),
                Path = context.Request.Path
            };

            return new EndpointDescription {
                EndpointUrl = url.Uri.ToString(),
                ProxyUrl = null,
                SecurityLevel = 0,
                SecurityMode = MessageSecurityMode.SignAndEncrypt,
                SecurityPolicyUri = policy,
                TransportProfileUri = Profiles.HttpsBinaryTransport,
                Server = null,
                UserIdentityTokens = null,
                ServerCertificate = null
            };
        }

        private readonly RequestDelegate _next;
        private readonly ITransportListenerCallback _callback;
        private readonly IMessageSerializer _encoder;
        private readonly string _listenerId;
    }
}
