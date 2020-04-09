// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Gateway.Server {
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Transport;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using Microsoft.Azure.IIoT.OpcUa.History;
    using Microsoft.Azure.IIoT.Auth.Server;
    using Microsoft.Azure.IIoT.Auth;
    using Serilog;
    using Opc.Ua;
    using Opc.Ua.Configuration;
    using Opc.Ua.Extensions;
    using Opc.Ua.Models;
    using Opc.Ua.Server;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using System.Text;
    using Microsoft.Azure.IIoT.Serializers;

    /// <summary>
    /// Gateway server controller implementation
    /// </summary>
    public sealed class GatewayServer : SessionServerBase, IServer {

        /// <inheritdoc/>
        public ITransportListenerCallback Callback => new SessionEndpoint(this);

        /// <inheritdoc/>
        public X509Certificate2 Certificate => InstanceCertificate;

        /// <inheritdoc/>
        public X509Certificate2Collection CertificateChain => InstanceCertificateChain;

        /// <inheritdoc/>
        public new ApplicationDescription ServerDescription => base.ServerDescription;

        /// <summary>
        /// Internal diagnostic data for the server
        /// </summary>
        public ServerDiagnosticsSummaryDataType ServerDiagnostics { get; }

        /// <summary>
        /// The session services used by the server
        /// </summary>
        public ISessionServices Sessions { get; private set; }

        /// <summary>
        /// Create server
        /// </summary>
        /// <param name="registry"></param>
        /// <param name="sessions"></param>
        /// <param name="nodes"></param>
        /// <param name="historian"></param>
        /// <param name="browser"></param>
        /// <param name="codec"></param>
        /// <param name="auth"></param>
        /// <param name="validator"></param>
        /// <param name="logger"></param>
        public GatewayServer(IApplicationRegistry registry, ISessionServices sessions,
            INodeServices<string> nodes, IHistoricAccessServices<string> historian,
            IBrowseServices<string> browser, IVariantEncoderFactory codec,
            IAuthConfig auth, ITokenValidator validator, ILogger logger) {

            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _codec = codec ?? throw new ArgumentNullException(nameof(codec));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _browser = browser ?? throw new ArgumentNullException(nameof(browser));
            _historian = historian ?? throw new ArgumentNullException(nameof(historian));
            _nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
            _auth = auth ?? throw new ArgumentNullException(nameof(auth));
            _validator = validator ?? throw new ArgumentNullException(nameof(_validator));

            Sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
            ServerDiagnostics = new ServerDiagnosticsSummaryDataType();
            ServerError = null;

            // Register with session services for diagnostics
            Sessions.SessionClosing += (s, _) => {
                lock (_lock) {
                    ServerDiagnostics.CurrentSessionCount--;
                }
            };
            Sessions.SessionCreated += (s, _) => {
                lock (_lock) {
                    ServerDiagnostics.CurrentSessionCount++;
                    ServerDiagnostics.CumulatedSessionCount++;
                }
            };
            Sessions.SessionTimeout += (s, _) => {
                lock (_lock) {
                    ServerDiagnostics.SessionTimeoutCount++;
                }
            };

            Sessions.ValidateUser += ValidateUserIdentityToken;
            _requestState = new RequestState();
            InitAsync().Wait(); // Initialize configuration and start underlying server
            _serverState = ServerState.Running;
            _endpoints = new EndpointDescriptionCollection();
        }

        /// <inheritdoc/>
        public void Register(EndpointDescriptionCollection endpoints) {
            if (endpoints == null) {
                throw new ArgumentNullException(nameof(endpoints));
            }

            foreach (var ep in endpoints.Select(e => (EndpointDescription)Utils.Clone(e))) {
                ep.Server = ServerDescription;
                ep.UserIdentityTokens = GetUserTokenPolicies(Configuration, ep);
                _endpoints.Add(ep);
            }

            // Update discovery urls
            ServerDescription.DiscoveryUrls = new StringCollection(_endpoints
                .Select(e => e.EndpointUrl)
                .Distinct());
        }

        /// <inheritdoc/>
        public void Unregister(EndpointDescriptionCollection endpoints) {
            if (endpoints == null) {
                throw new ArgumentNullException(nameof(endpoints));
            }
            foreach (var ep in _endpoints.ToList()) {
                foreach (var remove in endpoints) {
                    if (remove.SecurityLevel == ep.SecurityLevel &&
                        remove.TransportProfileUri == ep.TransportProfileUri) {
                        _endpoints.Remove(ep);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override ResponseHeader CreateSession(RequestHeader requestHeader,
            ApplicationDescription clientDescription, string serverUri, string endpointUrl,
            string sessionName, byte[] clientNonce, byte[] clientCertificate,
            double requestedSessionTimeout, uint maxResponseMessageSize,
            out NodeId sessionId, out NodeId authenticationToken,
            out double revisedSessionTimeout, out byte[] serverNonce,
            out byte[] serverCertificate,
            out EndpointDescriptionCollection serverEndpoints,
            out SignedSoftwareCertificateCollection serverSoftwareCertificates,
            out SignatureData serverSignature, out uint maxRequestMessageSize) {
            var context = OnRequestBegin(requestHeader, RequestType.CreateSession);
            try {
                var requireEncryption = clientCertificate != null || RequireEncryption(
                    context.ChannelContext.EndpointDescription);

                // validate client application instance certificate.
                X509Certificate2 clientX509Certificate = null;
                if (requireEncryption) {
                    clientX509Certificate = ValidateClientLeafCertificate(clientCertificate,
                        clientDescription.ApplicationUri, context.SecurityPolicyUri);
                }

                // verify the nonce provided by the client.
                if (clientNonce != null) {
                    if (clientNonce.Length < _minNonceLength) {
                        throw new ServiceResultException(StatusCodes.BadNonceInvalid);
                    }
                    // ignore nonce if security policy set to none
                    if (context.SecurityPolicyUri == SecurityPolicies.None) {
                        clientNonce = null;
                    }
                }

                serverEndpoints = GetEndpointsAsync(endpointUrl).Result;
                if (!serverEndpoints.Any()) {
                    throw new ServiceResultException(StatusCodes.BadNotConnected);
                }
                // Get our currently invoked endpoint
                var endpoint = context.ChannelContext.EndpointDescription;
                endpoint = serverEndpoints.FirstOrDefault(e =>
                    e.EndpointUrl == endpoint.EndpointUrl &&
                    e.SecurityMode == endpoint.SecurityMode &&
                    e.TransportProfileUri == endpoint.TransportProfileUri &&
                    e.SecurityPolicyUri == endpoint.SecurityPolicyUri);
                if (endpoint == null) {
                    throw new ServiceResultException(StatusCodes.BadTcpEndpointUrlInvalid);
                }
                // check the server uri to connect to through the gateway matches.
                if (!string.IsNullOrEmpty(serverUri)) {
                    if (serverUri != endpoint.Server.ApplicationUri) {
                        throw new ServiceResultException(StatusCodes.BadServerUriInvalid);
                    }
                }

                // create the session on the incoming endpoint.
                var session = Sessions.CreateSession(context, endpoint,
                    requireEncryption ? InstanceCertificate : null, clientNonce,
                    clientX509Certificate, requestedSessionTimeout,
                    out sessionId, out authenticationToken,
                    out serverNonce, out revisedSessionTimeout);

                // return the application instance certificate for the server.
                if (requireEncryption) {
                    // check if complete chain should be sent.
                    if (Configuration.SecurityConfiguration.SendCertificateChain &&
                        InstanceCertificateChain != null &&
                        InstanceCertificateChain.Count > 0) {
                        serverCertificate = InstanceCertificateChain.ToBytes();
                    }
                    else {
                        serverCertificate = InstanceCertificate.RawData;
                    }
                }
                else {
                    serverCertificate = null;
                }

                // return the software certificates assigned to the server.
                serverSoftwareCertificates = new SignedSoftwareCertificateCollection(
                    ServerProperties.SoftwareCertificates);
                // sign the nonce provided by the client if needed.
                if (clientX509Certificate != null && clientNonce != null) {
                    var dataToSign = Utils.Append(clientX509Certificate.RawData, clientNonce);
                    serverSignature = SecurityPolicies.Sign(InstanceCertificate,
                        context.SecurityPolicyUri, dataToSign);
                }
                else {
                    serverSignature = null;
                }
                _logger.Information("Session {sessionId} created.", sessionId);

                maxRequestMessageSize = (uint)MessageContext.MaxMessageSize;
                return CreateResponse(requestHeader, StatusCodes.Good);
            }
            catch (ServiceResultException e) {
                _logger.Error(e, "Creating session failed.");
                lock (_lock) {
                    ServerDiagnostics.RejectedSessionCount++;
                    ServerDiagnostics.RejectedRequestsCount++;
                    if (StatusCodeEx.IsSecurityError(e.StatusCode)) {
                        ServerDiagnostics.SecurityRejectedSessionCount++;
                        ServerDiagnostics.SecurityRejectedRequestsCount++;
                    }
                }
                throw;
            }
            finally {
                OnRequestComplete(context);
            }
        }

        /// <inheritdoc/>
        public override ResponseHeader ActivateSession(
            RequestHeader requestHeader, SignatureData clientSignature,
            SignedSoftwareCertificateCollection clientSoftwareCertificates,
            StringCollection localeIds, ExtensionObject userIdentityToken,
            SignatureData userTokenSignature, out byte[] serverNonce,
            out StatusCodeCollection results, out DiagnosticInfoCollection diagnosticInfos) {
            var context = OnRequestBegin(requestHeader, RequestType.ActivateSession);
            try {
                // First validate the provided certificates
                var clientCertificates = ValidateClientCertificateChain(context,
                    clientSoftwareCertificates, out results, out diagnosticInfos);

                // Activate session
                var identityChanged = Sessions.ActivateSession(context,
                    requestHeader.AuthenticationToken, clientSignature, clientCertificates,
                    userIdentityToken, userTokenSignature, localeIds, out serverNonce);
                if (identityChanged) {
                    _logger.Information("Session {SessionId} activated - identity changed.",
                        context.Session.Id);
                }
                else {
                    _logger.Debug("Session {SessionId} activated.", context.Session.Id);
                }
                return CreateResponse(requestHeader, StatusCodes.Good);
            }
            catch (ServiceResultException e) {
                _logger.Error(e, "Failed activating session.");
                lock (_lock) {
                    ServerDiagnostics.RejectedRequestsCount++;
                    if (StatusCodeEx.IsSecurityError(e.StatusCode)) {
                        ServerDiagnostics.SecurityRejectedRequestsCount++;
                    }
                }
                throw;
            }
            finally {
                OnRequestComplete(context);
            }
        }

        /// <inheritdoc/>
        public override ResponseHeader CloseSession(RequestHeader requestHeader,
            bool deleteSubscriptions) {
            var context = OnRequestBegin(requestHeader, RequestType.CloseSession);
            try {
                if (context.Session == null) {
                    throw new ServiceResultException(StatusCodes.BadSessionIdInvalid);
                }
                Sessions.CloseSession(context.Session.Id);
                _logger.Information("Session {SessionId} closed.", context.Session.Id);
                return CreateResponse(requestHeader, context.StringTable);
            }
            catch (ServiceResultException e) {
                _logger.Error(e, "Closing session failed.");
                lock (_lock) {
                    ServerDiagnostics.RejectedRequestsCount++;
                    if (StatusCodeEx.IsSecurityError(e.StatusCode)) {
                        ServerDiagnostics.SecurityRejectedRequestsCount++;
                    }
                }
                throw;
            }
            finally {
                OnRequestComplete(context);
            }
        }

        /// <inheritdoc/>
        public override ResponseHeader Cancel(RequestHeader requestHeader,
            uint requestHandle, out uint cancelCount) {
            var context = OnRequestBegin(requestHeader, RequestType.Cancel);
            try {
                _requestState.CancelRequests(requestHandle, out cancelCount);
                return CreateResponse(requestHeader, context.StringTable);
            }
            catch (ServiceResultException e) {
                _logger.Error(e, "Cancelling request failed.");
                lock (_lock) {
                    ServerDiagnostics.RejectedRequestsCount++;
                    if (StatusCodeEx.IsSecurityError(e.StatusCode)) {
                        ServerDiagnostics.SecurityRejectedRequestsCount++;
                    }
                }
                throw;
            }
            finally {
                OnRequestComplete(context);
            }
        }

        /// <inheritdoc/>
        public override ResponseHeader Browse(RequestHeader requestHeader,
            ViewDescription view, uint requestedMaxReferencesPerNode,
            BrowseDescriptionCollection nodesToBrowse,
            out BrowseResultCollection results,
            out DiagnosticInfoCollection diagnosticInfos) {
            var context = OnRequestBegin(requestHeader, RequestType.Browse);
            try {
                if (nodesToBrowse == null || nodesToBrowse.Count == 0) {
                    throw new ServiceResultException(StatusCodes.BadNothingToDo);
                }
                results = new BrowseResultCollection(LinqEx.Repeat(
                    () => (BrowseResult)null, nodesToBrowse.Count));
                diagnosticInfos = new DiagnosticInfoCollection(LinqEx.Repeat(
                    () => (DiagnosticInfo)null, nodesToBrowse.Count));
                BrowseAsync(context, requestHeader, view, requestedMaxReferencesPerNode,
                    nodesToBrowse, results, diagnosticInfos).Wait();

                return CreateResponse(requestHeader, context.StringTable);
            }
            catch (ServiceResultException e) {
                _logger.Error(e, "Browse failed.");
                lock (_lock) {
                    ServerDiagnostics.RejectedRequestsCount++;
                    if (StatusCodeEx.IsSecurityError(e.StatusCode)) {
                        ServerDiagnostics.SecurityRejectedRequestsCount++;
                    }
                }
                throw;
            }
            finally {
                OnRequestComplete(context);
            }
        }

        /// <inheritdoc/>
        public override ResponseHeader BrowseNext(RequestHeader requestHeader,
            bool releaseContinuationPoints, ByteStringCollection continuationPoints,
            out BrowseResultCollection results,
            out DiagnosticInfoCollection diagnosticInfos) {
            var context = OnRequestBegin(requestHeader, RequestType.BrowseNext);
            try {
                if (continuationPoints == null || continuationPoints.Count == 0) {
                    throw new ServiceResultException(StatusCodes.BadNothingToDo);
                }
                results = new BrowseResultCollection(LinqEx.Repeat(
                    () => (BrowseResult)null, continuationPoints.Count));
                diagnosticInfos = new DiagnosticInfoCollection(LinqEx.Repeat(
                    () => (DiagnosticInfo)null, continuationPoints.Count));
                BrowseNextAsync(context, requestHeader, releaseContinuationPoints,
                    continuationPoints, results, diagnosticInfos).Wait();

                return CreateResponse(requestHeader, context.StringTable);
            }
            catch (ServiceResultException e) {
                _logger.Error(e, "Browse Next failed.");
                lock (_lock) {
                    ServerDiagnostics.RejectedRequestsCount++;
                    if (StatusCodeEx.IsSecurityError(e.StatusCode)) {
                        ServerDiagnostics.SecurityRejectedRequestsCount++;
                    }
                }
                throw;
            }
            finally {
                OnRequestComplete(context);
            }
        }

        /// <inheritdoc/>
        public override ResponseHeader TranslateBrowsePathsToNodeIds(
            RequestHeader requestHeader,
            BrowsePathCollection browsePaths,
            out BrowsePathResultCollection results,
            out DiagnosticInfoCollection diagnosticInfos) {
            results = null;
            diagnosticInfos = null;

            var context = OnRequestBegin(requestHeader,
                RequestType.TranslateBrowsePathsToNodeIds);

            try {
                if (browsePaths == null || browsePaths.Count == 0) {
                    throw new ServiceResultException(StatusCodes.BadNothingToDo);
                }
                results = new BrowsePathResultCollection(LinqEx.Repeat(
                    () => (BrowsePathResult)null, browsePaths.Count));
                diagnosticInfos = new DiagnosticInfoCollection(LinqEx.Repeat(
                    () => (DiagnosticInfo)null, browsePaths.Count));
                BrowsePathAsync(context, requestHeader, browsePaths,
                    results, diagnosticInfos).Wait();

                return CreateResponse(requestHeader, context.StringTable);
            }
            catch (ServiceResultException e) {
                _logger.Error(e, "Translate Browse Paths failed.");
                lock (_lock) {
                    ServerDiagnostics.RejectedRequestsCount++;
                    if (StatusCodeEx.IsSecurityError(e.StatusCode)) {
                        ServerDiagnostics.SecurityRejectedRequestsCount++;
                    }
                }

                throw;
            }
            finally {
                OnRequestComplete(context);
            }
        }

        /// <inheritdoc/>
        public override ResponseHeader Read(RequestHeader requestHeader,
            double maxAge, TimestampsToReturn timestampsToReturn,
            ReadValueIdCollection nodesToRead, out DataValueCollection results,
            out DiagnosticInfoCollection diagnosticInfos) {
            var context = OnRequestBegin(requestHeader, RequestType.Read);
            try {
                if (nodesToRead == null || nodesToRead.Count == 0) {
                    throw new ServiceResultException(StatusCodes.BadNothingToDo);
                }
                results = new DataValueCollection(LinqEx.Repeat(
                    () => (DataValue)null, nodesToRead.Count));
                diagnosticInfos = new DiagnosticInfoCollection(LinqEx.Repeat(
                    () => (DiagnosticInfo)null, nodesToRead.Count));
                ReadAsync(context, requestHeader, maxAge, nodesToRead, results,
                    diagnosticInfos).Wait();

                return CreateResponse(requestHeader, context.StringTable);
            }
            catch (ServiceResultException e) {
                _logger.Error(e, "Read failed.");
                lock (_lock) {
                    ServerDiagnostics.RejectedRequestsCount++;
                    if (StatusCodeEx.IsSecurityError(e.StatusCode)) {
                        ServerDiagnostics.SecurityRejectedRequestsCount++;
                    }
                }
                throw;
            }
            finally {
                OnRequestComplete(context);
            }
        }

        /// <inheritdoc/>
        public override ResponseHeader Write(RequestHeader requestHeader,
            WriteValueCollection nodesToWrite, out StatusCodeCollection results,
            out DiagnosticInfoCollection diagnosticInfos) {
            var context = OnRequestBegin(requestHeader, RequestType.Write);

            try {
                if (nodesToWrite == null || nodesToWrite.Count == 0) {
                    throw new ServiceResultException(StatusCodes.BadNothingToDo);
                }

                results = new StatusCodeCollection(LinqEx.Repeat(
                    () => (StatusCode)StatusCodes.Good, nodesToWrite.Count));
                diagnosticInfos = new DiagnosticInfoCollection(LinqEx.Repeat(
                    () => (DiagnosticInfo)null, nodesToWrite.Count));
                WriteAsync(context, requestHeader, nodesToWrite, results,
                    diagnosticInfos).Wait();

                return CreateResponse(requestHeader, context.StringTable);
            }
            catch (ServiceResultException e) {
                _logger.Error(e, "Write failed.");
                lock (_lock) {
                    ServerDiagnostics.RejectedRequestsCount++;
                    if (StatusCodeEx.IsSecurityError(e.StatusCode)) {
                        ServerDiagnostics.SecurityRejectedRequestsCount++;
                    }
                }
                throw;
            }
            finally {
                OnRequestComplete(context);
            }
        }

        /// <inheritdoc/>
        public override ResponseHeader Call(RequestHeader requestHeader,
            CallMethodRequestCollection methodsToCall,
            out CallMethodResultCollection results,
            out DiagnosticInfoCollection diagnosticInfos) {
            var context = OnRequestBegin(requestHeader, RequestType.Call);
            try {
                if (methodsToCall == null || methodsToCall.Count == 0) {
                    throw new ServiceResultException(StatusCodes.BadNothingToDo);
                }

                results = new CallMethodResultCollection(LinqEx.Repeat(
                    () => (CallMethodResult)null, methodsToCall.Count));
                diagnosticInfos = new DiagnosticInfoCollection(LinqEx.Repeat(
                    () => (DiagnosticInfo)null, methodsToCall.Count));
                CallAsync(context, requestHeader, methodsToCall, results,
                    diagnosticInfos).Wait();

                return CreateResponse(requestHeader, context.StringTable);
            }
            catch (ServiceResultException e) {
                _logger.Error(e, "Call failed.");
                lock (_lock) {
                    ServerDiagnostics.RejectedRequestsCount++;
                    if (StatusCodeEx.IsSecurityError(e.StatusCode)) {
                        ServerDiagnostics.SecurityRejectedRequestsCount++;
                    }
                }
                throw;
            }
            finally {
                OnRequestComplete(context);
            }
        }
        /// <inheritdoc/>
        public override ResponseHeader HistoryRead(RequestHeader requestHeader,
            ExtensionObject historyReadDetails, TimestampsToReturn timestampsToReturn,
            bool releaseContinuationPoints, HistoryReadValueIdCollection nodesToRead,
            out HistoryReadResultCollection results,
            out DiagnosticInfoCollection diagnosticInfos) {
            var context = OnRequestBegin(requestHeader, RequestType.HistoryRead);
            try {
                if (nodesToRead == null || nodesToRead.Count == 0) {
                    throw new ServiceResultException(StatusCodes.BadNothingToDo);
                }
                results = new HistoryReadResultCollection(LinqEx.Repeat(
                    () => (HistoryReadResult)null, nodesToRead.Count));
                diagnosticInfos = new DiagnosticInfoCollection(LinqEx.Repeat(
                    () => (DiagnosticInfo)null, nodesToRead.Count));
                HistoryReadAsync(context, requestHeader, historyReadDetails,
                    releaseContinuationPoints, nodesToRead, results,
                    diagnosticInfos).Wait();

                return CreateResponse(requestHeader, context.StringTable);
            }
            catch (ServiceResultException e) {
                _logger.Error(e, "History Read failed.");
                lock (_lock) {
                    ServerDiagnostics.RejectedRequestsCount++;
                    if (StatusCodeEx.IsSecurityError(e.StatusCode)) {
                        ServerDiagnostics.SecurityRejectedRequestsCount++;
                    }
                }
                throw;
            }
            finally {
                OnRequestComplete(context);
            }
        }

        /// <inheritdoc/>
        public override ResponseHeader HistoryUpdate(RequestHeader requestHeader,
            ExtensionObjectCollection historyUpdateDetails,
            out HistoryUpdateResultCollection results,
            out DiagnosticInfoCollection diagnosticInfos) {
            var context = OnRequestBegin(requestHeader, RequestType.HistoryUpdate);
            try {
                if (historyUpdateDetails == null || historyUpdateDetails.Count == 0) {
                    throw new ServiceResultException(StatusCodes.BadNothingToDo);
                }
                results = new HistoryUpdateResultCollection(LinqEx.Repeat(
                    () => (HistoryUpdateResult)null, historyUpdateDetails.Count));
                diagnosticInfos = new DiagnosticInfoCollection(LinqEx.Repeat(
                    () => (DiagnosticInfo)null, historyUpdateDetails.Count));
                HistoryUpdateAsync(context, requestHeader, historyUpdateDetails,
                    results, diagnosticInfos).Wait();

                return CreateResponse(requestHeader, context.StringTable);
            }
            catch (ServiceResultException e) {
                _logger.Error(e, "History update failed.");
                lock (_lock) {
                    ServerDiagnostics.RejectedRequestsCount++;
                    if (StatusCodeEx.IsSecurityError(e.StatusCode)) {
                        ServerDiagnostics.SecurityRejectedRequestsCount++;
                    }
                }
                throw;
            }
            finally {
                OnRequestComplete(context);
            }
        }

        /// <inheritdoc/>
        public override ResponseHeader FindServers(RequestHeader requestHeader,
            string endpointUrl, StringCollection localeIds, StringCollection serverUris,
            out ApplicationDescriptionCollection servers) {
            ValidateRequest(requestHeader);

            var channelContext = SecureChannelContext.Current;
            if (endpointUrl == null) {
                endpointUrl = channelContext?.EndpointDescription?.EndpointUrl;
            }

            servers = FindServersAsync(endpointUrl, serverUris).Result;
            return CreateResponse(requestHeader, StatusCodes.Good);
        }

        /// <inheritdoc/>
        public override ResponseHeader GetEndpoints(RequestHeader requestHeader,
            string endpointUrl, StringCollection localeIds, StringCollection profileUris,
            out EndpointDescriptionCollection endpoints) {
            ValidateRequest(requestHeader);

            var channelContext = SecureChannelContext.Current;
            if (endpointUrl == null) {
                endpointUrl = channelContext?.EndpointDescription?.EndpointUrl;
            }

            endpoints = GetEndpointsAsync(endpointUrl, profileUris).Result;
            return CreateResponse(requestHeader, StatusCodes.Good);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing) {
            if (disposing) {
                OnServerStopping();
                Utils.SilentDispose(_requestState);
            }
            ServerError = null;
            base.Dispose(disposing);
            _requestState.Dispose();
        }

        /// <inheritdoc/>
        protected override void ValidateRequest(RequestHeader requestHeader) {
            // check for server error.
            var error = ServerError;
            if (ServiceResult.IsBad(error)) {
                throw new ServiceResultException(error);
            }
            // check for stopped
            if (_requestState == null) {
                throw new ServiceResultException(StatusCodes.BadServerHalted);
            }
            if (_serverState != ServerState.Running) {
                throw new ServiceResultException(StatusCodes.BadServerHalted);
            }
            base.ValidateRequest(requestHeader);
        }

        /// <summary>
        /// Find servers in registry
        /// </summary>
        /// <param name="endpointUrl"></param>
        /// <param name="serverUris"></param>
        /// <returns></returns>
        internal async Task<ApplicationDescriptionCollection> FindServersAsync(
            string endpointUrl, StringCollection serverUris) {
            IEnumerable<ApplicationInfoModel> applications;
            var url = Utils.ParseUri(endpointUrl);
            if (url == null) {
                return new ApplicationDescriptionCollection();
            }
            if (serverUris != null && serverUris.Count > 0) {
                var results = await Task.WhenAll(serverUris
                    .Select(uri => _registry.QueryAllApplicationsAsync(
                        new ApplicationRegistrationQueryModel {
                            ApplicationUri = uri
                        })));
                applications = results.SelectMany(t => t);
            }
            else {
                applications = await _registry.ListAllApplicationsAsync();
            }
            var apps = applications.Select(app => ToApplicationDescription(app,
                FormatTwinUri(endpointUrl, app.ApplicationId, null).ToString()));
            return new ApplicationDescriptionCollection(apps);
        }

        /// <summary>
        /// Get endpoints for the application from the registry and return
        /// an endpoint description collection.  Each endpoint is published
        /// onto each registered listener endpoint.
        /// </summary>
        /// <param name="discoveryUrl"></param>
        /// <param name="profileUris"></param>
        /// <returns></returns>
        internal async Task<EndpointDescriptionCollection> GetEndpointsAsync(
            string discoveryUrl, StringCollection profileUris = null) {
            ParseUri(discoveryUrl, out var applicationId, out var endpointId);
            if (string.IsNullOrEmpty(applicationId)) {
                return new EndpointDescriptionCollection();
            }
            var registration = await _registry.GetApplicationAsync(applicationId, true);
            // Make endpoints and publish on all transport endpoints
            var server = ToApplicationDescription(registration.Application);
            var endpoints = _endpoints.SelectMany(ep => {
                return registration.Endpoints
                    .Where(t => endpointId == null || t.Id == endpointId)
                    .Select(t => new EndpointDescription {
                        Server = server,
                        EndpointUrl = ToTwinUri(ep, applicationId, t.Id)
                            .ToString(),
                        SecurityLevel = ep.SecurityLevel,
                        SecurityMode = ep.SecurityMode,
                        SecurityPolicyUri = ep.SecurityPolicyUri,
                        TransportProfileUri = ep.TransportProfileUri,
                        ServerCertificate = ep.ServerCertificate,
                        ProxyUrl = ep.ProxyUrl,
                        UserIdentityTokens = GetUserTokenPolicies(t, ep)
                    });
            });
            // Filter from transports
            return new EndpointDescriptionCollection(endpoints
                .Where(e => profileUris == null || profileUris.Count == 0 ||
                    profileUris.Contains(e.TransportProfileUri)));
        }

        /// <summary>
        /// Browse on endpoint
        /// </summary>
        /// <param name="context"></param>
        /// <param name="requestHeader"></param>
        /// <param name="view"></param>
        /// <param name="requestedMaxReferencesPerNode"></param>
        /// <param name="nodesToBrowse"></param>
        /// <param name="results"></param>
        /// <param name="diagnosticInfos"></param>
        /// <returns></returns>
        internal async Task BrowseAsync(RequestContextModel context,
            RequestHeader requestHeader, ViewDescription view,
            uint requestedMaxReferencesPerNode, BrowseDescriptionCollection nodesToBrowse,
            BrowseResultCollection results, DiagnosticInfoCollection diagnosticInfos) {
            var endpointId = ToEndpointId(context.ChannelContext.EndpointDescription);
            var diagnostics = requestHeader.ToServiceModel();
            var codec = _codec.Create(context.Session.MessageContext);
            var elevation = GetRemoteCredentialsFromContext(context, codec.Serializer);
            for (var i = 0; i < nodesToBrowse.Count; i++) {
                try {
                    // Call service
                    var response = await _browser.NodeBrowseFirstAsync(endpointId,
                        new BrowseRequestModel {
                            NodeId = nodesToBrowse[i].NodeId
                                .AsString(context.Session.MessageContext),
                            Direction = nodesToBrowse[i].BrowseDirection
                                .ToServiceType(),
                            MaxReferencesToReturn = requestedMaxReferencesPerNode,
                            NoSubtypes = !nodesToBrowse[i].IncludeSubtypes,
                            ReferenceTypeId = nodesToBrowse[i].ReferenceTypeId
                                .AsString(context.Session.MessageContext),
                            View = view.ToServiceModel(
                                context.Session.MessageContext),
                            NodeClassFilter = ((Opc.Ua.NodeClass)nodesToBrowse[i].NodeClassMask)
                                .ToServiceMask(),
                            NodeIdsOnly = true,
                            Header = new RequestHeaderModel {
                                Diagnostics = diagnostics,
                                Elevation = elevation
                            }
                        });

                    // Update results
                    diagnosticInfos[i] = codec.Decode(response.ErrorInfo,
                        diagnostics, out var statusCode);

                    // Get references
                    var references = response.References?
                        .Select(r => new ReferenceDescription {
                            NodeId = r.Target.NodeId
                                .ToNodeId(context.Session.MessageContext),
                            BrowseName = r.Target.BrowseName
                                .ToQualifiedName(context.Session.MessageContext),
                            DisplayName = r.Target.DisplayName.ToLocalizedText(),
                            IsForward = r.Direction == Core.Models.BrowseDirection.Forward,
                            ReferenceTypeId = r.ReferenceTypeId
                                .ToNodeId(context.Session.MessageContext),
                            TypeDefinition = r.Target.TypeDefinitionId
                                .ToNodeId(context.Session.MessageContext),
                            NodeClass = r.Target.NodeClass?.ToStackType() ??
                                Opc.Ua.NodeClass.Unspecified
                        });

                    results[i] = new BrowseResult {
                        StatusCode = statusCode,
                        ContinuationPoint = response.ContinuationToken?.DecodeAsBase64(),
                        References = references == null ? null :
                            new ReferenceDescriptionCollection(references)
                    };
                }
                catch (Exception ex) {
                    diagnosticInfos[i] = new DiagnosticInfo(ex,
                        (DiagnosticsMasks)requestHeader.ReturnDiagnostics, true,
                        context.StringTable);
                    results[i] = new BrowseResult {
                        StatusCode = StatusCodes.BadMethodInvalid
                    };
                }
            }
        }

        /// <summary>
        /// Browse next on endpoint
        /// </summary>
        /// <param name="context"></param>
        /// <param name="requestHeader"></param>
        /// <param name="releaseContinuationPoints"></param>
        /// <param name="continuationPoints"></param>
        /// <param name="results"></param>
        /// <param name="diagnosticInfos"></param>
        /// <returns></returns>
        internal async Task BrowseNextAsync(RequestContextModel context,
            RequestHeader requestHeader, bool releaseContinuationPoints,
            ByteStringCollection continuationPoints, BrowseResultCollection results,
            DiagnosticInfoCollection diagnosticInfos) {
            var endpointId = ToEndpointId(context.ChannelContext.EndpointDescription);
            var diagnostics = requestHeader.ToServiceModel();
            var codec = _codec.Create(context.Session.MessageContext);
            var elevation = GetRemoteCredentialsFromContext(context, codec.Serializer);
            for (var i = 0; i < continuationPoints.Count; i++) {
                try {
                    // Call service
                    var response = await _browser.NodeBrowseNextAsync(endpointId,
                        new BrowseNextRequestModel {
                            ContinuationToken =
                                continuationPoints[i]?.ToBase64String(),
                            Abort = releaseContinuationPoints,
                            Header = new RequestHeaderModel {
                                Diagnostics = diagnostics,
                                Elevation = elevation
                            }
                        });

                    // Update results
                    diagnosticInfos[i] = codec.Decode(response.ErrorInfo,
                        diagnostics, out var statusCode);

                    // Get references
                    var references = response.References?
                        .Select(r => new ReferenceDescription {
                            NodeId = r.Target.NodeId
                                .ToNodeId(context.Session.MessageContext),
                            BrowseName = r.Target.BrowseName
                                .ToQualifiedName(context.Session.MessageContext),
                            DisplayName = r.Target.DisplayName.ToLocalizedText(),
                            IsForward = r.Direction == Core.Models.BrowseDirection.Forward,
                            ReferenceTypeId = r.ReferenceTypeId
                                .ToNodeId(context.Session.MessageContext),
                            TypeDefinition = r.Target.TypeDefinitionId
                                .ToNodeId(context.Session.MessageContext),
                            NodeClass = r.Target.NodeClass?.ToStackType() ??
                                Opc.Ua.NodeClass.Unspecified
                        });

                    results[i] = new BrowseResult {
                        StatusCode = statusCode,
                        ContinuationPoint = response.ContinuationToken?.DecodeAsBase64(),
                        References = references == null ? null :
                            new ReferenceDescriptionCollection(references)
                    };
                }
                catch (Exception ex) {
                    diagnosticInfos[i] = new DiagnosticInfo(ex,
                        (DiagnosticsMasks)requestHeader.ReturnDiagnostics, true,
                        context.StringTable);
                    results[i] = new BrowseResult {
                        StatusCode = StatusCodes.BadMethodInvalid
                    };
                }
            }
        }

        /// <summary>
        /// Call browse by path on endpoint
        /// </summary>
        /// <param name="context"></param>
        /// <param name="requestHeader"></param>
        /// <param name="browsePaths"></param>
        /// <param name="results"></param>
        /// <param name="diagnosticInfos"></param>
        /// <returns></returns>
        internal async Task BrowsePathAsync(RequestContextModel context,
            RequestHeader requestHeader, BrowsePathCollection browsePaths,
            BrowsePathResultCollection results, DiagnosticInfoCollection diagnosticInfos) {
            var endpointId = ToEndpointId(context.ChannelContext.EndpointDescription);
            var diagnostics = requestHeader.ToServiceModel();
            var codec = _codec.Create(context.Session.MessageContext);
            var elevation = GetRemoteCredentialsFromContext(context, codec.Serializer);
            for (var i = 0; i < browsePaths.Count; i++) {
                try {
                    // Call service
                    var response = await _browser.NodeBrowsePathAsync(endpointId,
                        new BrowsePathRequestModel {
                            NodeId = browsePaths[i].StartingNode
                                .AsString(context.Session.MessageContext),
                            BrowsePaths = new List<string[]> {
                                browsePaths[i].RelativePath
                                    .AsString(context.Session.MessageContext)
                            },
                            ReadVariableValues = false,
                            Header = new RequestHeaderModel {
                                Diagnostics = diagnostics,
                                Elevation = elevation
                            }
                        });

                    // Update results
                    diagnosticInfos[i] = codec.Decode(response.ErrorInfo,
                        diagnostics, out var statusCode);

                    // Get targets
                    var targets = response.Targets
                        .Select(r => new BrowsePathTarget {
                            RemainingPathIndex = (uint)(r.RemainingPathIndex ?? 0),
                            TargetId = r.Target.NodeId
                                .ToExpandedNodeId(context.Session.MessageContext)
                        });
                    results[i] = new BrowsePathResult {
                        StatusCode = statusCode,
                        Targets = targets == null ? null :
                            new BrowsePathTargetCollection(targets)
                    };
                }
                catch (Exception ex) {
                    diagnosticInfos[i] = new DiagnosticInfo(ex,
                        (DiagnosticsMasks)requestHeader.ReturnDiagnostics, true,
                        context.StringTable);
                    results[i] = new BrowsePathResult {
                        StatusCode = StatusCodes.BadBrowseNameInvalid
                    };
                }
            }
        }

        /// <summary>
        /// Call method on endpoint
        /// </summary>
        /// <param name="context"></param>
        /// <param name="requestHeader"></param>
        /// <param name="methodsToCall"></param>
        /// <param name="results"></param>
        /// <param name="diagnosticInfos"></param>
        /// <returns></returns>
        internal async Task CallAsync(RequestContextModel context,
            RequestHeader requestHeader, CallMethodRequestCollection methodsToCall,
            CallMethodResultCollection results, DiagnosticInfoCollection diagnosticInfos) {
            var endpointId = ToEndpointId(context.ChannelContext.EndpointDescription);
            var diagnostics = requestHeader.ToServiceModel();
            var codec = _codec.Create(context.Session.MessageContext);
            var elevation = GetRemoteCredentialsFromContext(context, codec.Serializer);
            for (var i = 0; i < methodsToCall.Count; i++) {
                try {
                    // Convert input arguments
                    var inputs = methodsToCall[i].InputArguments?
                        .Select(v => new MethodCallArgumentModel {
                            Value = codec.Encode(v, out var type),
                            DataType = type.ToString()
                        })
                        .ToList();

                    // Call service
                    var response = await _nodes.NodeMethodCallAsync(endpointId,
                        new MethodCallRequestModel {
                            MethodId = methodsToCall[i].MethodId.AsString(
                                context.Session.MessageContext),
                            ObjectId = methodsToCall[i].ObjectId.AsString(
                                context.Session.MessageContext),
                            Arguments = inputs,
                            Header = new RequestHeaderModel {
                                Diagnostics = diagnostics,
                                Elevation = elevation
                            }
                        });

                    // Update results
                    diagnosticInfos[i] = codec.Decode(response.ErrorInfo,
                        diagnostics, out var statusCode);

                    // Convert output arguments
                    var outputs = response.Results?
                        .Select(r => codec.Decode(r.Value, r.DataType));

                    results[i] = new CallMethodResult {
                        StatusCode = statusCode,
                        OutputArguments = outputs == null ? null :
                            new VariantCollection(outputs)
                    };
                }
                catch (Exception ex) {
                    diagnosticInfos[i] = new DiagnosticInfo(ex,
                        (DiagnosticsMasks)requestHeader.ReturnDiagnostics, true,
                        context.StringTable);
                    results[i] = new CallMethodResult {
                        StatusCode = StatusCodes.BadMethodInvalid
                    };
                }
            }
        }

        /// <summary>
        /// Read from endpoint
        /// </summary>
        /// <param name="context"></param>
        /// <param name="requestHeader"></param>
        /// <param name="maxAge"></param>
        /// <param name="nodesToRead"></param>
        /// <param name="results"></param>
        /// <param name="diagnosticInfos"></param>
        /// <returns></returns>
        internal async Task ReadAsync(RequestContextModel context,
            RequestHeader requestHeader, double maxAge,
            ReadValueIdCollection nodesToRead, DataValueCollection results,
            DiagnosticInfoCollection diagnosticInfos) {
            var endpointId = ToEndpointId(context.ChannelContext.EndpointDescription);
            var diagnostics = requestHeader.ToServiceModel();
            var codec = _codec.Create(context.Session.MessageContext);
            var elevation = GetRemoteCredentialsFromContext(context, codec.Serializer);

            var batch = new ReadRequestModel {
                Attributes = new List<AttributeReadRequestModel>(),
                Header = new RequestHeaderModel {
                    Diagnostics = diagnostics,
                    Elevation = elevation
                }
            };
            for (var i = 0; i < nodesToRead.Count; i++) {
                if (nodesToRead[i].AttributeId == Attributes.Value) {
                    // Read value using value read
                    try {
                        nodesToRead[i].Processed = true;
                        // Call service
                        var response = await _nodes.NodeValueReadAsync(endpointId,
                            new ValueReadRequestModel {
                                IndexRange = nodesToRead[i].IndexRange,
                                NodeId = nodesToRead[i].NodeId.AsString(
                                    context.Session.MessageContext),
                                MaxAge = TimeSpan.FromMilliseconds(maxAge),
                                Header = new RequestHeaderModel {
                                    Diagnostics = diagnostics,
                                    Elevation = elevation
                                }
                            });

                        // Update results
                        diagnosticInfos[i] = codec.Decode(response.ErrorInfo,
                            diagnostics, out var statusCode);

                        var value = codec.Decode(response.Value, response.DataType);
                        results[i] = new DataValue(value, statusCode,
                            response.SourceTimestamp ?? DateTime.MinValue,
                            response.ServerTimestamp ?? DateTime.MinValue) {
                            ServerPicoseconds = response.ServerPicoseconds ?? 0,
                            SourcePicoseconds = response.SourcePicoseconds ?? 0
                        };
                    }
                    catch (Exception ex) {
                        _logger.Error(ex, "Node value read failed.");

                        // nodesToRead[i].Processed = false;

                        diagnosticInfos[i] = new DiagnosticInfo(ex,
                            (DiagnosticsMasks)requestHeader.ReturnDiagnostics, true,
                            context.StringTable);
                        results[i] = new DataValue(StatusCodes.BadNotReadable);
                    }
                }

                if (!nodesToRead[i].Processed) {
                    // Read attribute using batch
                    batch.Attributes.Add(new AttributeReadRequestModel {
                        Attribute = (NodeAttribute)nodesToRead[i].AttributeId,
                        NodeId = nodesToRead[i].NodeId.AsString(context.Session.MessageContext)
                    });
                }
            }
            if (batch.Attributes.Count == 0) {
                Debug.Assert(nodesToRead.All(n => n.Processed));
                Debug.Assert(results.All(r => r != null));
            }
            else {
                try {
                    // Do batch read
                    var batchResponse = await _nodes.NodeReadAsync(endpointId, batch);
                    if ((batchResponse.Results?.Count ?? 0) != batch.Attributes.Count) {
                        // Batch response is missing results
                        throw new IndexOutOfRangeException("Read response is missing results");
                    }
                    var index = 0;
                    for (var i = 0; i < nodesToRead.Count; i++) {
                        if (!nodesToRead[i].Processed) {
                            var response = batchResponse.Results[index++];
                            diagnosticInfos[i] = codec.Decode(response.ErrorInfo,
                                diagnostics, out var statusCode);
                            var value = codec.Decode(response.Value,
                                AttributeMap.GetBuiltInType(nodesToRead[i].AttributeId));
                            results[i] = new DataValue(value, statusCode, DateTime.MinValue,
                                DateTime.UtcNow);
                        }
                    }
                }
                catch (Exception ex) {
                    _logger.Error(ex, "Node read as batch failed.");
                    for (var i = 0; i < nodesToRead.Count; i++) {
                        if (results[i] != null) {
                            // Only fill in what wasnt yet given a value.
                            continue;
                        }
                        diagnosticInfos[i] = new DiagnosticInfo(ex,
                            (DiagnosticsMasks)requestHeader.ReturnDiagnostics, true,
                                context.StringTable);
                        results[i] = new DataValue(StatusCodes.BadNotReadable);
                    }
                }
            }
        }

        /// <summary>
        /// Write to endpoint
        /// </summary>
        /// <param name="context"></param>
        /// <param name="requestHeader"></param>
        /// <param name="nodesToWrite"></param>
        /// <param name="results"></param>
        /// <param name="diagnosticInfos"></param>
        /// <returns></returns>
        internal async Task WriteAsync(RequestContextModel context,
            RequestHeader requestHeader, WriteValueCollection nodesToWrite,
            StatusCodeCollection results, DiagnosticInfoCollection diagnosticInfos) {
            var endpointId = ToEndpointId(context.ChannelContext.EndpointDescription);
            var diagnostics = requestHeader.ToServiceModel();
            var codec = _codec.Create(context.Session.MessageContext);
            var elevation = GetRemoteCredentialsFromContext(context, codec.Serializer);

            var batch = new WriteRequestModel {
                Attributes = new List<AttributeWriteRequestModel>(),
                Header = new RequestHeaderModel {
                    Diagnostics = diagnostics,
                    Elevation = elevation
                }
            };
            for (var i = 0; i < nodesToWrite.Count; i++) {
                if (nodesToWrite[i].AttributeId == Attributes.Value) {
                    // Read value using value read
                    try {
                        nodesToWrite[i].Processed = true;
                        // Call service
                        var response = await _nodes.NodeValueWriteAsync(endpointId,
                            new ValueWriteRequestModel {
                                IndexRange = nodesToWrite[i].IndexRange,
                                NodeId = nodesToWrite[i].NodeId.AsString(
                                    context.Session.MessageContext),
                                DataType = nodesToWrite[i].TypeId.AsString(
                                    context.Session.MessageContext),
                                Value = codec.Encode(
                                    nodesToWrite[i].Value.WrappedValue, out var type),
                                Header = new RequestHeaderModel {
                                    Diagnostics = diagnostics,
                                    Elevation = elevation
                                }
                            });

                        // Update results
                        diagnosticInfos[i] = codec.Decode(response.ErrorInfo,
                            diagnostics, out var statusCode);
                        results[i] = statusCode;
                    }
                    catch (Exception ex) {
                        _logger.Error(ex, "Node value write failed.");

                        // nodesToWrite[i].Processed = false;

                        diagnosticInfos[i] = new DiagnosticInfo(ex,
                            (DiagnosticsMasks)requestHeader.ReturnDiagnostics, true,
                            context.StringTable);
                        results[i] = StatusCodes.BadNotWritable;
                    }
                }

                if (!nodesToWrite[i].Processed) {
                    // Read attribute using batch
                    batch.Attributes.Add(new AttributeWriteRequestModel {
                        Attribute = (NodeAttribute)nodesToWrite[i].AttributeId,
                        NodeId = nodesToWrite[i].NodeId.AsString(context.Session.MessageContext),
                        Value = codec.Encode(
                            nodesToWrite[i].Value.WrappedValue, out var type)
                    });
                }
            }
            if (batch.Attributes.Count == 0) {
                Debug.Assert(nodesToWrite.All(n => n.Processed));
            }
            else {
                try {
                    // Do batch write
                    var batchResponse = await _nodes.NodeWriteAsync(endpointId, batch);
                    if ((batchResponse.Results?.Count ?? 0) != batch.Attributes.Count) {
                        // Batch response is missing results
                        throw new IndexOutOfRangeException("Write response is missing results");
                    }
                    var index = 0;
                    for (var i = 0; i < nodesToWrite.Count; i++) {
                        if (!nodesToWrite[i].Processed) {
                            var response = batchResponse.Results[index++];
                            diagnosticInfos[i] = codec.Decode(response.ErrorInfo,
                                diagnostics, out var statusCode);
                            results[i] = statusCode;
                        }
                    }
                }
                catch (Exception ex) {
                    _logger.Error(ex, "Node write as batch failed.");
                    for (var i = 0; i < nodesToWrite.Count; i++) {
                        if (nodesToWrite[i].Processed) {
                            continue;
                        }
                        diagnosticInfos[i] = new DiagnosticInfo(ex,
                            (DiagnosticsMasks)requestHeader.ReturnDiagnostics, true,
                                context.StringTable);
                        results[i] = StatusCodes.BadNotWritable;
                    }
                }
            }
        }

        /// <summary>
        /// Read history
        /// </summary>
        /// <param name="context"></param>
        /// <param name="requestHeader"></param>
        /// <param name="historyReadDetails"></param>
        /// <param name="releaseContinuationPoints"></param>
        /// <param name="nodesToRead"></param>
        /// <param name="results"></param>
        /// <param name="diagnosticInfos"></param>
        /// <returns></returns>
        internal async Task HistoryReadAsync(RequestContextModel context,
            RequestHeader requestHeader, ExtensionObject historyReadDetails,
            bool releaseContinuationPoints, HistoryReadValueIdCollection nodesToRead,
            HistoryReadResultCollection results, DiagnosticInfoCollection diagnosticInfos) {
            var endpointId = ToEndpointId(context.ChannelContext.EndpointDescription);
            var diagnostics = requestHeader.ToServiceModel();
            var codec = _codec.Create(context.Session.MessageContext);
            var elevation = GetRemoteCredentialsFromContext(context, codec.Serializer);
            for (var i = 0; i < nodesToRead.Count; i++) {
                try {
                    if (nodesToRead[i].ContinuationPoint == null) {
                        // Call read first
                        var response = await _historian.HistoryReadAsync(endpointId,
                            new HistoryReadRequestModel<VariantValue> {
                                NodeId = nodesToRead[i].NodeId
                                    .AsString(context.Session.MessageContext),
                                IndexRange = nodesToRead[i].IndexRange,
                                Details = historyReadDetails == null ? null :
                                    codec.Encode(new Variant(historyReadDetails), out var tmp),
                                Header = new RequestHeaderModel {
                                    Diagnostics = diagnostics,
                                    Elevation = elevation
                                }
                            });

                        // Update results
                        diagnosticInfos[i] = codec.Decode(response.ErrorInfo,
                            diagnostics, out var statusCode);

                        // Collect response
                        results[i] = new HistoryReadResult {
                            StatusCode = statusCode,
                            ContinuationPoint = response.ContinuationToken?.DecodeAsBase64(),
                            HistoryData = response.History == null ? null : (ExtensionObject)
                                codec.Decode(response.History, BuiltInType.ExtensionObject).Value
                        };
                    }
                    else {
                        // Continue reading
                        var response = await _historian.HistoryReadNextAsync(endpointId,
                            new HistoryReadNextRequestModel {
                                ContinuationToken = nodesToRead[i].ContinuationPoint
                                    .ToBase64String(),
                                Abort = !releaseContinuationPoints ? (bool?)null : true,
                                Header = new RequestHeaderModel {
                                    Diagnostics = diagnostics,
                                    Elevation = elevation
                                }
                            });

                        // Update results
                        diagnosticInfos[i] = codec.Decode(response.ErrorInfo,
                            diagnostics, out var statusCode);

                        // Collect response
                        results[i] = new HistoryReadResult {
                            StatusCode = statusCode,
                            ContinuationPoint = response.ContinuationToken?.DecodeAsBase64(),
                            HistoryData = response.History == null ? null : (ExtensionObject)
                                codec.Decode(response.History, BuiltInType.ExtensionObject).Value
                        };
                    }
                }
                catch (Exception ex) {
                    diagnosticInfos[i] = new DiagnosticInfo(ex,
                        (DiagnosticsMasks)requestHeader.ReturnDiagnostics, true,
                        context.StringTable);
                    results[i] = new HistoryReadResult {
                        StatusCode = StatusCodes.BadMethodInvalid
                    };
                }
            }
        }

        /// <summary>
        /// Update history
        /// </summary>
        /// <param name="context"></param>
        /// <param name="requestHeader"></param>
        /// <param name="historyUpdateDetails"></param>
        /// <param name="results"></param>
        /// <param name="diagnosticInfos"></param>
        /// <returns></returns>
        internal async Task HistoryUpdateAsync(RequestContextModel context,
            RequestHeader requestHeader, ExtensionObjectCollection historyUpdateDetails,
            HistoryUpdateResultCollection results, DiagnosticInfoCollection diagnosticInfos) {
            var endpointId = ToEndpointId(context.ChannelContext.EndpointDescription);
            var diagnostics = requestHeader.ToServiceModel();
            var codec = _codec.Create(context.Session.MessageContext);
            var elevation = GetRemoteCredentialsFromContext(context, codec.Serializer);
            for (var i = 0; i < historyUpdateDetails.Count; i++) {
                try {
                    // Call service
                    var response = await _historian.HistoryUpdateAsync(endpointId,
                        new HistoryUpdateRequestModel<VariantValue> {
                            Details = historyUpdateDetails == null ? null :
                                codec.Encode(new Variant(historyUpdateDetails), out var tmp),
                            Header = new RequestHeaderModel {
                                Diagnostics = diagnostics,
                                Elevation = elevation
                            }
                        });

                    // Update results
                    diagnosticInfos[i] = codec.Decode(response.ErrorInfo,
                            diagnostics, out var statusCode);

                    // Collect response
                    results[i] = new HistoryUpdateResult {
                        StatusCode = statusCode,
                        OperationResults = response.Results
                            .Select(r => new StatusCode(r.StatusCode ?? StatusCodes.Good))
                            .ToArray()
                    };
                }
                catch (Exception ex) {
                    diagnosticInfos[i] = new DiagnosticInfo(ex,
                        (DiagnosticsMasks)requestHeader.ReturnDiagnostics, true,
                        context.StringTable);
                    results[i] = new HistoryUpdateResult {
                        StatusCode = StatusCodes.BadMethodInvalid
                    };
                }
            }
        }

        /// <summary>
        /// Get remote credential from top of the identities stack if any.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        private static CredentialModel GetRemoteCredentialsFromContext(
            RequestContextModel context, IJsonSerializer serializer) {
            if (!context.Session.Identities.Any()) {
                return null; // no credential - anonymous access - throw?
            }
            if (context.Session.Identities.Count > 1) {
                // This is remote credential
                return context.Session.Identities[1].ToServiceModel(serializer);
            }
            return null;
        }

        /// <summary>
        /// Parse the endpoint id out of the endpoint description.  The endpoint url is
        /// assumed to end in the endpoint identifier.  This is what we use.
        /// </summary>
        /// <param name="endpointDescription"></param>
        /// <returns></returns>
        private string ToEndpointId(EndpointDescription endpointDescription) {
            if (endpointDescription == null) {
                throw new ArgumentNullException(nameof(endpointDescription));
            }
            if (string.IsNullOrEmpty(endpointDescription.EndpointUrl)) {
                throw new ArgumentNullException(nameof(endpointDescription.EndpointUrl));
            }
            ParseUri(endpointDescription.EndpointUrl, out var tmp, out var endpointId);
            return endpointId;
        }

        /// <summary>
        /// Convert to endpoint uri
        /// </summary>
        /// <param name="endpointDescription"></param>
        /// <param name="applicationId"></param>
        /// <param name="endpointId"></param>
        /// <returns></returns>
        private static Uri ToTwinUri(EndpointDescription endpointDescription,
            string applicationId, string endpointId = null) {
            if (endpointDescription == null) {
                throw new ArgumentNullException(nameof(endpointDescription));
            }
            if (string.IsNullOrEmpty(endpointDescription.EndpointUrl)) {
                throw new ArgumentNullException(nameof(endpointDescription.EndpointUrl));
            }
            return FormatTwinUri(endpointDescription.EndpointUrl, applicationId, endpointId);
        }

        /// <summary>
        /// Parse the application and endpoint ids out of the endpoint url.
        /// The endpoint url is assumed to end in the endpoint identifier.
        /// </summary>
        /// <param name="endpointUrl"></param>
        /// <param name="applicationId"></param>
        /// <param name="endpointId"></param>
        /// <returns>base uri</returns>
        private string ParseUri(string endpointUrl, out string applicationId,
            out string endpointId) {
            if (string.IsNullOrEmpty(endpointUrl)) {
                throw new ArgumentNullException(nameof(endpointUrl));
            }
            applicationId = null;
            endpointId = null;
            var url = Utils.ParseUri(endpointUrl);
            var path = url.AbsolutePath.Split(new char[] { '/' },
                StringSplitOptions.RemoveEmptyEntries);
            foreach (var segment in path.Reverse()) {
                if (segment.EqualsIgnoreCase("endpoint")) {
                    endpointId = applicationId;
                    applicationId = null;
                }
                else if (segment.EqualsIgnoreCase("applications")) {
                    if (applicationId != null) {
                        return endpointUrl.Substring(0, endpointUrl.IndexOf(segment,
                            StringComparison.InvariantCultureIgnoreCase) - 1);
                    }
                    break;
                }
                else if (applicationId == null) {
                    applicationId = segment;
                }
                else {
                    break;
                }
            }
            // Malformed url
            applicationId = null;
            endpointId = null;
            return endpointUrl;
        }

        /// <summary>
        /// Format endpoint uri
        /// </summary>
        /// <param name="endpointUrl"></param>
        /// <param name="applicationId"></param>
        /// <param name="endpointId"></param>
        /// <returns></returns>
        private static Uri FormatTwinUri(string endpointUrl,
            string applicationId, string endpointId) {
            if (string.IsNullOrEmpty(endpointUrl)) {
                throw new ArgumentNullException(nameof(endpointUrl));
            }
            var builder = new UriBuilder(endpointUrl);
            var path = builder.Path;
            var split = path.IndexOf("/applications/",
                StringComparison.InvariantCultureIgnoreCase);
            if (split != -1) {
                path = path.Substring(0, split);
            }
            path = path.TrimEnd('/');
            if (!string.IsNullOrEmpty(applicationId)) {
                path += "/applications/" + applicationId;
            }
            if (!string.IsNullOrEmpty(endpointId)) {
                path += "/endpoint/" + endpointId;
            }
            builder.Path = path;
            return builder.Uri;
        }

        /// <summary>
        /// Convert to application discription.
        /// </summary>
        /// <param name="application"></param>
        /// <param name="discoveryUrl"></param>
        /// <returns></returns>
        private ApplicationDescription ToApplicationDescription(
            ApplicationInfoModel application, string discoveryUrl = null) {
            return new ApplicationDescription {
                ApplicationName =
                    application.ApplicationName,
                ApplicationType =
                    application.ApplicationType.ToStackType(),
                ApplicationUri =
                    application.ApplicationUri,
                DiscoveryProfileUri =
                    application.DiscoveryProfileUri,
                ProductUri =
                    application.ProductUri,
                DiscoveryUrls = new StringCollection(discoveryUrl != null ?
                    discoveryUrl.YieldReturn() : _endpoints
                        .Select(e => ToTwinUri(e, application.ApplicationId)
                            .ToString())
                        .Distinct()),
                GatewayServerUri = ServerDescription.ApplicationUri
            };
        }

        /// <summary>
        /// Begins a request
        /// </summary>
        /// <param name="requestHeader">The request header.</param>
        /// <param name="requestType">Type of the request.</param>
        /// <returns></returns>
        internal RequestContextModel OnRequestBegin(RequestHeader requestHeader,
            RequestType requestType) {
            ValidateRequest(requestHeader);

            // Get operation context from session manager
            var context = Sessions.GetContext(requestHeader, requestType);
            _logger.Debug("{type} {id} validated.", context.RequestType, context.RequestId);

            // Pass to request manager
            lock (_lock) {
                if (_requestState == null) {
                    throw new ServiceResultException(StatusCodes.BadServerHalted);
                }
                _requestState.RequestReceived(context);
            }
            return context;
        }

        /// <summary>
        /// Completes the request
        /// </summary>
        /// <param name="context">The operation context.</param>
        internal void OnRequestComplete(RequestContextModel context) {
            lock (_lock) {
                if (_requestState == null) {
                    throw new ServiceResultException(StatusCodes.BadServerHalted);
                }
                _requestState.RequestCompleted(context);
            }
        }

        /// <summary>
        /// Initialize server from application configuration
        /// </summary>
        private async Task InitAsync() {
            var config = new ApplicationConfiguration {
                ApplicationName = "Opc UA Gateway Server",
                ApplicationType = Opc.Ua.ApplicationType.ClientAndServer,
                ApplicationUri =
                    $"urn:{Utils.GetHostName()}:Microsoft:OpcGatewayServer",
                ProductUri = "http://opcfoundation.org/UA/SampleServer",

                SecurityConfiguration = new SecurityConfiguration {
                    ApplicationCertificate = new CertificateIdentifier {
                        StoreType = "Directory",
                        StorePath =
                "OPC Foundation/CertificateStores/MachineDefault",
                        SubjectName = "Opc UA Gateway Server"
                    },
                    TrustedPeerCertificates = new CertificateTrustList {
                        StoreType = "Directory",
                        StorePath =
                "OPC Foundation/CertificateStores/UA Applications",
                    },
                    TrustedIssuerCertificates = new CertificateTrustList {
                        StoreType = "Directory",
                        StorePath =
                "OPC Foundation/CertificateStores/UA Certificate Authorities",
                    },
                    RejectedCertificateStore = new CertificateTrustList {
                        StoreType = "Directory",
                        StorePath =
                "OPC Foundation/CertificateStores/RejectedCertificates",
                    },
                    AutoAcceptUntrustedCertificates = false
                },
                TransportConfigurations = new TransportConfigurationCollection(),
                TransportQuotas = TransportQuotaConfigEx.DefaultTransportQuotas(),
                ServerConfiguration = new ServerConfiguration {
                    ServerProfileArray = new StringCollection {
                        "Local Discovery Server Profile"
                    },
                    ServerCapabilities = new StringCollection {
                        "LDS"
                    },
                    SupportedPrivateKeyFormats = new StringCollection {
                        "PFX", "PEM"
                    },
                    // Runtime configuration
                    BaseAddresses = new StringCollection(),
                    SecurityPolicies = new ServerSecurityPolicyCollection {
                        new ServerSecurityPolicy {
                            SecurityMode = MessageSecurityMode.SignAndEncrypt,
                            SecurityPolicyUri = SecurityPolicies.Basic256Sha256,
                        },
                        new ServerSecurityPolicy {
                            SecurityMode = MessageSecurityMode.None,
                            SecurityPolicyUri = SecurityPolicies.None
                        }
                    },
                    UserTokenPolicies = new UserTokenPolicyCollection {
                        new UserTokenPolicy {
                            TokenType = UserTokenType.Anonymous,
                            SecurityPolicyUri = SecurityPolicies.None
                        },
                        new UserTokenPolicy {
                            TokenType = UserTokenType.UserName,
                            SecurityPolicyUri = SecurityPolicies.Basic256Sha256
                        },
                        new UserTokenPolicy {
                            TokenType = UserTokenType.IssuedToken
                        }
                    }
                },
                TraceConfiguration = new TraceConfiguration {
                    TraceMasks = 1
                }
            };

            _logger.Information("Starting server...");
            ApplicationInstance.MessageDlg = new DummyDialog();

            config = ApplicationInstance.FixupAppConfig(config);
            await config.Validate(Opc.Ua.ApplicationType.Server);
            config.CertificateValidator.CertificateValidation += (v, e) => {
                if (e.Error.StatusCode ==
                    StatusCodes.BadCertificateUntrusted) {
                    e.Accept = true; // TODO
                    _logger.Information((e.Accept ? "Accepted" : "Rejected") +
                        " Certificate {Subject}", e.Certificate.Subject);
                }
            };

            await config.CertificateValidator.Update(config.SecurityConfiguration);
            // Use existing certificate, if it is there.
            var cert = await config.SecurityConfiguration.ApplicationCertificate
                .Find(true);
            if (cert == null) {
                // Create cert
#pragma warning disable IDE0067 // Dispose objects before losing scope
                cert = CertificateFactory.CreateCertificate(
                    config.SecurityConfiguration.ApplicationCertificate.StoreType,
                    config.SecurityConfiguration.ApplicationCertificate.StorePath,
                    null, config.ApplicationUri, config.ApplicationName,
                    config.SecurityConfiguration.ApplicationCertificate.SubjectName,
                    null, CertificateFactory.defaultKeySize,
                    DateTime.UtcNow - TimeSpan.FromDays(1),
                    CertificateFactory.defaultLifeTime,
                    CertificateFactory.defaultHashSize,
                    false, null, null);
#pragma warning restore IDE0067 // Dispose objects before losing scope
            }

            if (cert != null) {
                config.SecurityConfiguration.ApplicationCertificate.Certificate = cert;
                config.ApplicationUri = Utils.GetApplicationUriFromCertificate(cert);
            }

            var application = new ApplicationInstance(config);

            // check the application certificate.
            var haveAppCertificate =
                await application.CheckApplicationInstanceCertificate(false, 0);
            if (!haveAppCertificate) {
                throw new Exception(
                    "Application instance certificate invalid!");
            }

            Start(config);
            // Calls StartApplication
            // Calls InitializeServiceHosts (see below)
            _minNonceLength = config.SecurityConfiguration.NonceLength;
        }

        /// <inheritdoc/>
        private class DummyDialog : IApplicationMessageDlg {
            /// <inheritdoc/>
            public override void Message(string text, bool ask) { }
            /// <inheritdoc/>
            public override Task<bool> ShowAsync() {
                return Task.FromResult(true);
            }
        }

        /// <inheritdoc/>
        protected override IList<Task> InitializeServiceHosts(
            ApplicationConfiguration configuration,
            out ApplicationDescription serverDescription,
            out EndpointDescriptionCollection endpoints) {
            // set server description - will be returned by ServerDescription property.
            serverDescription = new ApplicationDescription {
                ApplicationUri = configuration.ApplicationUri,
                ApplicationName = new LocalizedText("en-US", configuration.ApplicationName),
                ApplicationType = configuration.ApplicationType,
                ProductUri = configuration.ProductUri,
                DiscoveryProfileUri = null,
                GatewayServerUri = null,
                DiscoveryUrls = null
            };
            endpoints = _endpoints;
            return new List<Task> { Task.CompletedTask };
        }

        /// <inheritdoc/>
        protected override void OnServerStopping() {
            // Called when server is stopped or disposed.
            _serverState = ServerState.Shutdown;
        }

        /// <inheritdoc/>
        protected override UserTokenPolicyCollection GetUserTokenPolicies(
            ApplicationConfiguration configuration, EndpointDescription description) {
            var policies = new UserTokenPolicyCollection();

            // Allow anonymous access through the endpoints
            if (!_auth.AuthRequired) {
                policies.Add(new UserTokenPolicy {
                    PolicyId = kGatewayPolicyPrefix + "Anonymous",
                    TokenType = UserTokenType.Anonymous,
                    SecurityPolicyUri = SecurityPolicies.None
                });
            }

            // Authenticate and then use endpoints
            if (!string.IsNullOrEmpty(_auth.InstanceUrl)) {
                policies.Add(new UserTokenPolicy {
                    PolicyId = kGatewayPolicyPrefix + "Jwt",
                    TokenType = UserTokenType.IssuedToken,
                    IssuedTokenType = "http://opcfoundation.org/UA/UserToken#JWT",
                    IssuerEndpointUrl = _auth.InstanceUrl,
                    SecurityPolicyUri =
                        description.SecurityMode == MessageSecurityMode.None ?
                            SecurityPolicies.Basic256Sha256 : SecurityPolicies.None
                });
            }
            return policies;
        }

        /// <summary>
        /// Advertise the correct user auth policies based on endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        private UserTokenPolicyCollection GetUserTokenPolicies(EndpointRegistrationModel endpoint,
            EndpointDescription description) {
            if (endpoint == null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            if (description == null) {
                throw new ArgumentNullException(nameof(description));
            }

            // Get gateway specific policies
            var policies = GetUserTokenPolicies(Configuration, description);

            // Add endpoint policies from registration model
            var endpointPolicies = endpoint.AuthenticationMethods.ToStackModel();
            if (endpointPolicies != null) {
                policies.AddRange(endpointPolicies);
            }
            return policies;
        }

        /// <summary>
        /// Called to validate identity token
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void ValidateUserIdentityToken(object sender, UserIdentityHandlerArgs args) {
            if (args == null) {
                throw new ArgumentNullException(nameof(args));
            }
            if (!(sender is IServerSession session)) {
                return; // need session access
            }
            if (args.ValidationException != null) {
                return; // Another handler failed already - no need to continue.
            }
            if (args.Token == null) {
                args.Token = new AnonymousIdentityToken();
            }
            if (string.IsNullOrEmpty(args.Token.PolicyId)) {
                args.Token.PolicyId = "Anonymous";
            }
            if (args.Token.PolicyId.StartsWith(kGatewayPolicyPrefix, StringComparison.Ordinal) ||
                args.CurrentIdentities.Count == 0) {
                switch (args.Token) {
                    case AnonymousIdentityToken at:
                        if (_auth.AuthRequired) {
                            args.ValidationException = ServiceResultException.Create(
                                StatusCodes.BadIdentityTokenRejected,
                                    "Anonymous session not allowed against gateway server.");
                        }
                        break;
                    case IssuedIdentityToken it:
                        if (it.IssuedTokenType != IssuedTokenType.JWT) {
                            args.ValidationException = ServiceResultException.Create(
                                StatusCodes.BadIdentityTokenRejected,
                                    "Token type not supported on gateway server.");
                        }
                        else if (string.IsNullOrEmpty(_auth.TrustedIssuer)) {
                            args.ValidationException = ServiceResultException.Create(
                                StatusCodes.BadIdentityTokenRejected,
                                    "Gateway server not configured for JWT authentication.");
                        }
                        else {
                            var validatedToken = _validator.ValidateAsync(
                                Encoding.UTF8.GetString(it.DecryptedTokenData)).Result;
                            if (validatedToken != null) {
                                // Success. TODO: do something with the validated token
                                break;
                            }
                            args.ValidationException = ServiceResultException.Create(
                                StatusCodes.BadIdentityTokenRejected,
                                    "Invalid JWT token provided.");
                        }
                        break;
                    case UserNameIdentityToken unt:
                        args.ValidationException = ServiceResultException.Create(
                            StatusCodes.BadIdentityTokenRejected,
                                "Username password validation not supported on gateway.");
                        break;
                    case X509IdentityToken x509:
                        args.ValidationException = ServiceResultException.Create(
                            StatusCodes.BadIdentityTokenRejected,
                                "x509 certificate validation not supported on gateway.");
                        break;
                    default:
                        args.ValidationException = ServiceResultException.Create(
                            StatusCodes.BadIdentityTokenRejected,
                                $"Token type {args.Token?.GetType()} not supported.");
                        break;
                }
                if (args.ValidationException == null) {
                    // Top of stack is remote elevation - root is gateway identity
                    var newIdentities = new List<IUserIdentity> {
                        new UserIdentity(args.Token)
                    };
                    if (args.CurrentIdentities.Count > 1) {
                        // Replaced gateway auth token through policy id
                        newIdentities.Add(args.CurrentIdentities[1]);
                    }
                    else if (args.Token is AnonymousIdentityToken &&
                        args.CurrentIdentities.Count != 0) {
                        // Drop down to 0 to reauth to gateway
                        args.NewIdentities = new List<IUserIdentity>();
                    }
                    else {
                        // new identities set.
                        args.NewIdentities = newIdentities;
                    }
                }
                return;
            }
            try {
                //
                // Test remote credential on server side by doing a simple batch read with
                // the token.
                //
                var batchResponse = _nodes.NodeReadAsync(ToEndpointId(session.Endpoint),
                    new ReadRequestModel {
                        Attributes = new List<AttributeReadRequestModel> {
                            new AttributeReadRequestModel {
                                Attribute = NodeAttribute.NodeClass,
                                NodeId = "i=85"
                            }
                        },
                        Header = new RequestHeaderModel {
                            Elevation = args.Token.ToServiceModel(_codec.Default.Serializer)
                        }
                    }).Result;
                if ((batchResponse.Results?.Count ?? 0) != 1) {
                    // Batch response is missing results
                    args.ValidationException = ServiceResultException.Create(
                        StatusCodes.BadInternalError,
                            "Remote server responded with insufficient results " +
                            "when trying to validate token is accepted.");
                }
                else {
                    var response = batchResponse.Results[0];
                    var status = response.ErrorInfo?.StatusCode ?? StatusCodes.Good;
                    if (status == StatusCodes.Good) {
                        // Authentication was also successful using the token.
                        var newIdentities = new List<IUserIdentity> {
                            args.CurrentIdentities[0]
                        };
                        if (!(args.Token is AnonymousIdentityToken)) {
                            newIdentities.Add(new UserIdentity(args.Token));
                        }
                        args.NewIdentities = newIdentities;
                    }
                    else {
                        args.ValidationException = ServiceResultException.Create(status,
                            "Token rejected by remote server.");
                    }
                }
            }
            catch (Exception ex) {
                args.ValidationException = ServiceResultException.Create(
                    StatusCodes.BadIdentityTokenRejected, ex,
                        "Token rejected by remote server.");
            }
        }

        /// <summary>
        /// Validate client certificates provided during activation and
        /// fill diagnostics.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="clientSoftwareCertificates"></param>
        /// <param name="results"></param>
        /// <param name="diagnosticInfos"></param>
        /// <returns></returns>
        private List<SoftwareCertificate> ValidateClientCertificateChain(
            RequestContextModel context,
            SignedSoftwareCertificateCollection clientSoftwareCertificates,
            out StatusCodeCollection results, out DiagnosticInfoCollection diagnosticInfos) {
            results = null;
            diagnosticInfos = null;
            // validate client's software certificates.
            var softwareCertificates = new List<SoftwareCertificate>();
            if (context.SecurityPolicyUri != SecurityPolicies.None) {
                var diagnosticsExist = false;
                if ((context.DiagnosticsMask & DiagnosticsMasks.OperationAll) != 0) {
                    diagnosticInfos = new DiagnosticInfoCollection();
                }
                results = new StatusCodeCollection();
                diagnosticInfos = new DiagnosticInfoCollection();
                foreach (var signedCertificate in clientSoftwareCertificates) {
                    var result = SoftwareCertificate.Validate(CertificateValidator,
                        signedCertificate.CertificateData, out var softwareCertificate);
                    if (ServiceResult.IsBad(result)) {
                        results.Add(result.Code);
                        // add diagnostics if requested.
                        if ((context.DiagnosticsMask & DiagnosticsMasks.OperationAll) != 0) {
                            diagnosticInfos.Add(new DiagnosticInfo(result,
                                context.DiagnosticsMask, false, context.StringTable));
                            diagnosticsExist = true;
                        }
                    }
                    else {
                        softwareCertificates.Add(softwareCertificate);
                        results.Add(StatusCodes.Good);
                        // add diagnostics if requested.
                        if ((context.DiagnosticsMask & DiagnosticsMasks.OperationAll) != 0) {
                            diagnosticInfos.Add(null);
                        }
                    }
                }
                if (!diagnosticsExist && diagnosticInfos != null) {
                    diagnosticInfos.Clear();
                }
            }
            // check if certificates meet the server's requirements.
            // ValidateSoftwareCertificates(softwareCertificates);
            return softwareCertificates;
        }

        /// <summary>
        /// Validates the leaf certificate meets the client description
        /// </summary>
        /// <param name="clientCertificate"></param>
        /// <param name="applicationUri"></param>
        /// <param name="securityPolicyUri"></param>
        /// <returns></returns>
        private X509Certificate2 ValidateClientLeafCertificate(byte[] clientCertificate,
            string applicationUri, string securityPolicyUri) {
            if (clientCertificate == null) {
                throw new ArgumentNullException(nameof(clientCertificate));
            }
            if (string.IsNullOrEmpty(securityPolicyUri)) {
                throw new ArgumentNullException(nameof(securityPolicyUri));
            }
            try {
                // Parse chain from certificate
                var clientCertificateChain = Utils.ParseCertificateChainBlob(
                    clientCertificate);
                if (clientCertificateChain.Count == 0) {
                    throw ServiceResultException.Create(
                        StatusCodes.BadCertificateInvalid,
                        "The certificate chain is empty.");
                }
                var parsedClientCertificate = clientCertificateChain[0];
                if (securityPolicyUri == SecurityPolicies.None) {
                    return null;
                }
                //
                // verify if applicationUri from ApplicationDescription matches
                // the applicationUri in the client certificate.
                //
                var certificateApplicationUri =
                Utils.GetApplicationUriFromCertificate(parsedClientCertificate);
                if (!string.IsNullOrEmpty(certificateApplicationUri) &&
                    !string.IsNullOrEmpty(applicationUri) &&
                    certificateApplicationUri != applicationUri) {
                    throw ServiceResultException.Create(
                        StatusCodes.BadCertificateUriInvalid,
                        "The client's applicationUri from ApplicationDescription " +
                        "does not match the Uri in the Certificate.");
                }

                // Validate certificate
                CertificateValidator.Validate(clientCertificateChain);
                return parsedClientCertificate;
            }
            catch (ServiceResultException) {
                throw;
            }
            catch (Exception e) {
                throw new ServiceResultException(new ServiceResult(e));
            }
        }

        private const string kGatewayPolicyPrefix = "$Gateway_Auth_";
        private readonly object _lock = new object();
        private readonly IApplicationRegistry _registry;
        private readonly IVariantEncoderFactory _codec;
        private readonly ILogger _logger;
        private readonly IBrowseServices<string> _browser;
        private readonly IHistoricAccessServices<string> _historian;
        private readonly INodeServices<string> _nodes;
        private readonly IAuthConfig _auth;
        private readonly ITokenValidator _validator;
        private readonly RequestState _requestState;
        private readonly EndpointDescriptionCollection _endpoints;
        private int _minNonceLength;
        private ServerState _serverState;
    }
}
