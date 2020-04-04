// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Transport {
    using Microsoft.AspNetCore.Http;
    using Opc.Ua;
    using Serilog;
    using System;
    using System.Threading.Tasks;
    using System.Linq;
    using Autofac;

    /// <summary>
    /// Enables websocket middleware to pass sockets on to listener
    /// </summary>
    public interface IHttpChannelListener {

        /// <summary>
        /// Middleware entry point
        /// </summary>
        /// <param name="context"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<IServiceResponse> ProcessAsync(HttpContext context,
            IServiceRequest request);
    }

    /// <summary>
    /// Decodes and forwards http based requests to the server and returns
    /// server responses to clients.
    /// </summary>
    public class HttpChannelListener : IHttpChannelListener, IStartable,
        IDisposable {

        /// <summary>
        /// Creates middleware to forward requests to controller
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public HttpChannelListener(IServer controller,
            IWebListenerConfig config, ILogger logger) {
            if (controller?.Callback == null) {
                throw new ArgumentNullException(nameof(controller));
            }
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            _listenerId = Guid.NewGuid().ToString();
            _urls = (config?.ListenUrls?.Length ?? 0) != 0 ? config.ListenUrls :
                new string[] { "http://localhost:9040" };
            _controller = controller;
        }

        /// <inheritdoc/>
        public void Start() {
            _controller.Register(GetEndpoints());
        }

        /// <inheritdoc/>
        public void Dispose() {
            _controller.Unregister(GetEndpoints());
        }

        /// <inheritdoc/>
        public async Task<IServiceResponse> ProcessAsync(HttpContext context,
            IServiceRequest request) {
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
            var ep = GetEndpointFromContext(context);
            // Forward to listener / server
            return await Task.Factory.FromAsync(
                (callback, state) => _controller.Callback.BeginProcessRequest(
                    _listenerId, ep, request, callback, state),
                _controller.Callback.EndProcessRequest,
                TaskCreationOptions.DenyChildAttach);
        }

        /// <summary>
        /// Build the endpoint information from context and configuration.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private EndpointDescription GetEndpointFromContext(HttpContext context) {

            // Select security policy contained in header, or reject
            EndpointDescription ep;
            var policy = SecurityPolicies.Https;
            if (context.Request.Headers.TryGetValue("OPCUA-SecurityPolicy",
                out var header)) {
                policy = header;
                ep = GetEndpoints().FirstOrDefault(e => e.SecurityPolicyUri == policy);
                if (ep == null) {
                    _logger.Debug("Policy {policy} not supported", policy);
                    // Policy not supported.
                    return null;
                }
            }
            else {
                ep = GetEndpoints().First();
            }
            // Todo: enforce https

            // Build endpoint url
            var scheme = context.Request.Scheme;
            var url = new UriBuilder {
                Scheme = scheme,
                Host = context.Request.Host.Host ?? Utils.GetHostName(),
                Port = context.Request.Host.Port ??
                    (scheme.EqualsIgnoreCase(Utils.UriSchemeHttps) ?
                        443 : 80),
                Path = context.Request.Path
            };
            return new EndpointDescription {
                EndpointUrl = url.Uri.ToString(),
                SecurityLevel = ep.SecurityLevel,
                SecurityMode = ep.SecurityMode,
                SecurityPolicyUri = policy,
                TransportProfileUri = ep.TransportProfileUri,
                ServerCertificate = ep.ServerCertificate,
                ProxyUrl = null,
                Server = null,
                UserIdentityTokens = null
            };
        }

        /// <summary>
        /// Get all endpoints
        /// </summary>
        /// <returns></returns>
        private EndpointDescriptionCollection GetEndpoints() {
            return new EndpointDescriptionCollection(_urls
                .Select(url => new EndpointDescription {
                    EndpointUrl = url,
                    SecurityLevel = 1,
                    SecurityMode = MessageSecurityMode.None,
                    SecurityPolicyUri = SecurityPolicies.None,
                    ServerCertificate = null,
                    Server = _controller.ServerDescription,
                    UserIdentityTokens = null,
                    ProxyUrl = null,
                    TransportProfileUri = Profiles.HttpsBinaryTransport
                }));
        }

        private readonly IServer _controller;
        private readonly ILogger _logger;
        private readonly string _listenerId;
        private readonly string[] _urls;
    }
}
