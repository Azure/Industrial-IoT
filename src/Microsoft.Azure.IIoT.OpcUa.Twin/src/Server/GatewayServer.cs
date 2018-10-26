// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin.Server {
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using Opc.Ua.Models;
    using Opc.Ua.Server;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    /// <summary>
    /// The standard implementation of a UA server.
    /// </summary>
    public class GatewayServer : SessionServerBase {

        /// <summary>
        /// Internal diagnostic data for the server
        /// </summary>
        public ServerDiagnosticsSummaryDataType ServerDiagnostics { get; }

        /// <summary>
        /// The session services used by the server
        /// </summary>
        public ISessionServices SessionServices { get; private set; }

        /// <summary>
        /// Create server
        /// </summary>
        /// <param name="registry"></param>
        /// <param name="sessions"></param>
        /// <param name="twin"></param>
        /// <param name="browser"></param>
        /// <param name="encoder"></param>
        /// <param name="logger"></param>
        public GatewayServer(IApplicationRegistry registry, ISessionServices sessions,
            INodeServices<string> twin, IBrowseServices<string> browser, IVariantEncoder encoder,
            ILogger logger) {

            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _browser = browser ?? throw new ArgumentNullException(nameof(browser));
            _twin = twin ?? throw new ArgumentNullException(nameof(twin));

            ServerDiagnostics = new ServerDiagnosticsSummaryDataType();
            SessionServices = sessions;
            _serverState = ServerState.Shutdown;
        }

        /// <inheritdoc/>
        public override ResponseHeader CreateSession(RequestHeader requestHeader,
            ApplicationDescription clientDescription, string serverUri, string endpointUrl,
            string sessionName, byte[] clientNonce, byte[] clientCertificate,
            double requestedSessionTimeout, uint maxResponseMessageSize,
            out NodeId sessionId, out NodeId authenticationToken, out double revisedSessionTimeout,
            out byte[] serverNonce, out byte[] serverCertificate,
            out EndpointDescriptionCollection serverEndpoints,
            out SignedSoftwareCertificateCollection serverSoftwareCertificates,
            out SignatureData serverSignature, out uint maxRequestMessageSize) {
            var context = OnRequestBegin(requestHeader, RequestType.CreateSession);
            try {
                // check the server uri.
                if (!string.IsNullOrEmpty(serverUri)) {
                    if (serverUri != Configuration.ApplicationUri) {
                        throw new ServiceResultException(StatusCodes.BadServerUriInvalid);
                    }
                }

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
                    if (clientNonce.Length < _configuration.SecurityConfiguration.NonceLength) {
                        throw new ServiceResultException(StatusCodes.BadNonceInvalid);
                    }
                    // ignore nonce if security policy set to none
                    if (context.SecurityPolicyUri == SecurityPolicies.None) {
                        clientNonce = null;
                    }
                }

                // create the session.
                var session = SessionServices.CreateSession(context, requireEncryption ?
                    InstanceCertificate : null, clientNonce, clientX509Certificate,
                    requestedSessionTimeout, out sessionId, out authenticationToken,
                    out serverNonce, out revisedSessionTimeout);
                // return the application instance certificate for the server.
                if (requireEncryption) {
                    // check if complete chain should be sent.
                    if (Configuration.SecurityConfiguration.SendCertificateChain &&
                        InstanceCertificateChain != null &&
                        InstanceCertificateChain.Count > 0) {
                        var serverCertificateChain = new List<byte>();
                        for (var i = 0; i < InstanceCertificateChain.Count; i++) {
                            serverCertificateChain.AddRange(
                                InstanceCertificateChain[i].RawData);
                        }
                        serverCertificate = serverCertificateChain.ToArray();
                    }
                    else {
                        serverCertificate = InstanceCertificate.RawData;
                    }
                }
                else {
                    serverCertificate = null;
                }

                // return the endpoints supported by the server.
                serverEndpoints = GetEndpointsAsync(endpointUrl).Result;
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
                _logger.Info($"Session {sessionId} created. ");

                maxRequestMessageSize = (uint)MessageContext.MaxMessageSize;
                return CreateResponse(requestHeader, StatusCodes.Good);
            }
            catch (ServiceResultException e) {
                _logger.Error("Creating session failed.", e);
                lock (_lock) {
                    ServerDiagnostics.RejectedSessionCount++;
                    ServerDiagnostics.RejectedRequestsCount++;
                    if (StatusCodeEx.IsSecurityError(e.StatusCode)) {
                        ServerDiagnostics.SecurityRejectedSessionCount++;
                        ServerDiagnostics.SecurityRejectedRequestsCount++;
                    }
                }
                throw e;
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
                var identityChanged = SessionServices.ActivateSession(context,
                    requestHeader.AuthenticationToken, clientSignature, clientCertificates,
                    userIdentityToken, userTokenSignature, localeIds, out serverNonce);
                if (identityChanged) {
                    _logger.Info($"Session {context.Session.Id} activated - identity changed.");
                }
                else {
                    _logger.Debug($"Session {context.Session.Id} activated.");
                }
                return CreateResponse(requestHeader, StatusCodes.Good);
            }
            catch (ServiceResultException e) {
                _logger.Error("Failed activating session.", e);
                lock (_lock) {
                    ServerDiagnostics.RejectedRequestsCount++;
                    if (StatusCodeEx.IsSecurityError(e.StatusCode)) {
                        ServerDiagnostics.SecurityRejectedRequestsCount++;
                    }
                }
                throw e;
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
                SessionServices.CloseSession(context.Session.Id);
                _logger.Info($"Session {context.Session.Id} closed. ");
                return CreateResponse(requestHeader, context.StringTable);
            }
            catch (ServiceResultException e) {
                lock (_lock) {
                    ServerDiagnostics.RejectedRequestsCount++;
                    if (StatusCodeEx.IsSecurityError(e.StatusCode)) {
                        ServerDiagnostics.SecurityRejectedRequestsCount++;
                    }
                }
                throw e;
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
                _requestManager.CancelRequests(requestHandle, out cancelCount);
                return CreateResponse(requestHeader, context.StringTable);
            }
            catch (ServiceResultException e) {
                lock (_lock) {
                    ServerDiagnostics.RejectedRequestsCount++;
                    if (StatusCodeEx.IsSecurityError(e.StatusCode)) {
                        ServerDiagnostics.SecurityRejectedRequestsCount++;
                    }
                }
                throw e;
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
                results = new BrowseResultCollection(EnumerableEx.Repeat(
                    () => (BrowseResult)null, nodesToBrowse.Count));
                diagnosticInfos = new DiagnosticInfoCollection(EnumerableEx.Repeat(
                    () => (DiagnosticInfo)null, nodesToBrowse.Count));
                BrowseAsync(context, requestHeader, view, requestedMaxReferencesPerNode,
                    nodesToBrowse, results, diagnosticInfos).Wait();

                return CreateResponse(requestHeader, context.StringTable);
            }
            catch (ServiceResultException e) {
                lock (_lock) {
                    ServerDiagnostics.RejectedRequestsCount++;
                    if (StatusCodeEx.IsSecurityError(e.StatusCode)) {
                        ServerDiagnostics.SecurityRejectedRequestsCount++;
                    }
                }
                throw e;
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
                results = new BrowseResultCollection(EnumerableEx.Repeat(
                    () => (BrowseResult)null, continuationPoints.Count));
                diagnosticInfos = new DiagnosticInfoCollection(EnumerableEx.Repeat(
                    () => (DiagnosticInfo)null, continuationPoints.Count));
                BrowseNextAsync(context, requestHeader, releaseContinuationPoints,
                    continuationPoints, results, diagnosticInfos).Wait();

                return CreateResponse(requestHeader, context.StringTable);
            }
            catch (ServiceResultException e) {
                lock (_lock) {
                    ServerDiagnostics.RejectedRequestsCount++;
                    if (StatusCodeEx.IsSecurityError(e.StatusCode)) {
                        ServerDiagnostics.SecurityRejectedRequestsCount++;
                    }
                }
                throw e;
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

            var context = OnRequestBegin(requestHeader, RequestType.TranslateBrowsePathsToNodeIds);

            try {
                if (browsePaths == null || browsePaths.Count == 0) {
                    throw new ServiceResultException(StatusCodes.BadNothingToDo);
                }

                //m_serverInternal.NodeManager.TranslateBrowsePathsToNodeIds(
                //    context,
                //    browsePaths,
                //    out results,
                //    out diagnosticInfos);

                return CreateResponse(requestHeader, context.StringTable);
            }
            catch (ServiceResultException e) {
                lock (_lock) {
                    ServerDiagnostics.RejectedRequestsCount++;

                    if (StatusCodeEx.IsSecurityError(e.StatusCode)) {
                        ServerDiagnostics.SecurityRejectedRequestsCount++;
                    }
                }

                throw e;
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

                results = new DataValueCollection(EnumerableEx.Repeat(
                    () => (DataValue)null, nodesToRead.Count));
                diagnosticInfos = new DiagnosticInfoCollection(EnumerableEx.Repeat(
                    () => (DiagnosticInfo)null, nodesToRead.Count));
                ReadAsync(context, requestHeader, maxAge, nodesToRead, results,
                    diagnosticInfos).Wait();

                return CreateResponse(requestHeader, context.StringTable);
            }
            catch (ServiceResultException e) {
                lock (_lock) {
                    ServerDiagnostics.RejectedRequestsCount++;
                    if (StatusCodeEx.IsSecurityError(e.StatusCode)) {
                        ServerDiagnostics.SecurityRejectedRequestsCount++;
                    }
                }
                throw e;
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

                results = new StatusCodeCollection(EnumerableEx.Repeat(
                    () => (StatusCode)StatusCodes.Good, nodesToWrite.Count));
                diagnosticInfos = new DiagnosticInfoCollection(EnumerableEx.Repeat(
                    () => (DiagnosticInfo)null, nodesToWrite.Count));
                WriteAsync(context, requestHeader, nodesToWrite, results,
                    diagnosticInfos).Wait();

                return CreateResponse(requestHeader, context.StringTable);
            }
            catch (ServiceResultException e) {
                lock (_lock) {
                    ServerDiagnostics.RejectedRequestsCount++;
                    if (StatusCodeEx.IsSecurityError(e.StatusCode)) {
                        ServerDiagnostics.SecurityRejectedRequestsCount++;
                    }
                }
                throw e;
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

                results = new CallMethodResultCollection(EnumerableEx.Repeat(
                    () => (CallMethodResult)null, methodsToCall.Count));
                diagnosticInfos = new DiagnosticInfoCollection(EnumerableEx.Repeat(
                    () => (DiagnosticInfo)null, methodsToCall.Count));
                CallAsync(context, requestHeader, methodsToCall, results,
                    diagnosticInfos).Wait();

                return CreateResponse(requestHeader, context.StringTable);
            }
            catch (ServiceResultException e) {
                lock (_lock) {
                    ServerDiagnostics.RejectedRequestsCount++;
                    if (StatusCodeEx.IsSecurityError(e.StatusCode)) {
                        ServerDiagnostics.SecurityRejectedRequestsCount++;
                    }
                }
                throw e;
            }
            finally {
                OnRequestComplete(context);
            }
        }

#if FALSE

        /// <inheritdoc/>
        public override ResponseHeader HistoryRead(
            RequestHeader requestHeader,
            ExtensionObject historyReadDetails,
            TimestampsToReturn timestampsToReturn,
            bool releaseContinuationPoints,
            HistoryReadValueIdCollection nodesToRead,
            out HistoryReadResultCollection results,
            out DiagnosticInfoCollection diagnosticInfos) {
            var context = OnRequestBegin(requestHeader, RequestType.HistoryRead);

            try {
                if (nodesToRead == null || nodesToRead.Count == 0) {
                    throw new ServiceResultException(StatusCodes.BadNothingToDo);
                }

                // TODO

                return CreateResponse(requestHeader, context.StringTable);
            }
            catch (ServiceResultException e) {
                lock (_lock) {
                    ServerDiagnostics.RejectedRequestsCount++;
                    if (StatusCodeEx.IsSecurityError(e.StatusCode)) {
                        ServerDiagnostics.SecurityRejectedRequestsCount++;
                    }
                }
                throw e;
            }
            finally {
                OnRequestComplete(context);
            }
        }

        /// <inheritdoc/>
        public override ResponseHeader HistoryUpdate(
            RequestHeader requestHeader,
            ExtensionObjectCollection historyUpdateDetails,
            out HistoryUpdateResultCollection results,
            out DiagnosticInfoCollection diagnosticInfos) {
            var context = OnRequestBegin(requestHeader, RequestType.HistoryUpdate);

            try {
                if (historyUpdateDetails == null || historyUpdateDetails.Count == 0) {
                    throw new ServiceResultException(StatusCodes.BadNothingToDo);
                }

                // TODO

                return CreateResponse(requestHeader, context.StringTable);
            }
            catch (ServiceResultException e) {
                lock (_lock) {
                    ServerDiagnostics.RejectedRequestsCount++;
                    if (StatusCodeEx.IsSecurityError(e.StatusCode)) {
                        ServerDiagnostics.SecurityRejectedRequestsCount++;
                    }
                }
                throw e;
            }
            finally {
                OnRequestComplete(context);
            }
        }

        /// <inheritdoc/>
        public override ResponseHeader CreateSubscription(RequestHeader requestHeader,
            double requestedPublishingInterval, uint requestedLifetimeCount,
            uint requestedMaxKeepAliveCount, uint maxNotificationsPerPublish,
            bool publishingEnabled, byte priority,
            out uint subscriptionId, out double revisedPublishingInterval,
            out uint revisedLifetimeCount, out uint revisedMaxKeepAliveCount) {
            var context = ValidateRequest(requestHeader, RequestType.CreateSubscription);

            try {
                // TODO

                return CreateResponse(requestHeader, context.StringTable);
            }
            catch (ServiceResultException e) {
                lock (_lock) {
                    ServerDiagnostics.RejectedRequestsCount++;
                    if (StatusCodeEx.IsSecurityError(e.StatusCode)) {
                        ServerDiagnostics.SecurityRejectedRequestsCount++;
                    }
                }
                throw e;
            }
            finally {
                OnRequestComplete(context);
            }
        }

        /// <inheritdoc/>
        public override ResponseHeader DeleteSubscriptions(
            RequestHeader requestHeader,
            UInt32Collection subscriptionIds,
            out StatusCodeCollection results,
            out DiagnosticInfoCollection diagnosticInfos) {
            var context = ValidateRequest(requestHeader, RequestType.DeleteSubscriptions);

            try {
                if (subscriptionIds == null || subscriptionIds.Count == 0) {
                    throw new ServiceResultException(StatusCodes.BadNothingToDo);
                }

                // TODO

                return CreateResponse(requestHeader, context.StringTable);
            }
            catch (ServiceResultException e) {
                lock (_lock) {
                    ServerDiagnostics.RejectedRequestsCount++;

                    if (StatusCodeEx.IsSecurityError(e.StatusCode)) {
                        ServerDiagnostics.SecurityRejectedRequestsCount++;
                    }
                }

                throw e;
            }
            finally {
                OnRequestComplete(context);
            }
        }

        /// <inheritdoc/>
        public override ResponseHeader Publish(
            RequestHeader requestHeader,
            SubscriptionAcknowledgementCollection subscriptionAcknowledgements,
            out uint subscriptionId,
            out UInt32Collection availableSequenceNumbers,
            out bool moreNotifications,
            out NotificationMessage notificationMessage,
            out StatusCodeCollection results,
            out DiagnosticInfoCollection diagnosticInfos) {
            var context = ValidateRequest(requestHeader, RequestType.Publish);

            try {
                // TODO

                return CreateResponse(requestHeader, context.StringTable);
            }
            catch (ServiceResultException e) {
                lock (_lock) {
                    ServerDiagnostics.RejectedRequestsCount++;

                    if (StatusCodeEx.IsSecurityError(e.StatusCode)) {
                        ServerDiagnostics.SecurityRejectedRequestsCount++;
                    }
                }

                throw e;
            }
            finally {
                OnRequestComplete(context);
            }
        }

        /// <inheritdoc/>
        public override ResponseHeader Republish(
            RequestHeader requestHeader,
            uint subscriptionId,
            uint retransmitSequenceNumber,
            out NotificationMessage notificationMessage) {
            var context = ValidateRequest(requestHeader, RequestType.Republish);

            try {
                // TODO

                return CreateResponse(requestHeader, context.StringTable);
            }
            catch (ServiceResultException e) {
                lock (_lock) {
                    ServerDiagnostics.RejectedRequestsCount++;

                    if (StatusCodeEx.IsSecurityError(e.StatusCode)) {
                        ServerDiagnostics.SecurityRejectedRequestsCount++;
                    }
                }

                throw e;
            }
            finally {
                OnRequestComplete(context);
            }
        }

        /// <inheritdoc/>
        public override ResponseHeader ModifySubscription(
            RequestHeader requestHeader,
            uint subscriptionId,
            double requestedPublishingInterval,
            uint requestedLifetimeCount,
            uint requestedMaxKeepAliveCount,
            uint maxNotificationsPerPublish,
            byte priority,
            out double revisedPublishingInterval,
            out uint revisedLifetimeCount,
            out uint revisedMaxKeepAliveCount) {
            var context = ValidateRequest(requestHeader, RequestType.ModifySubscription);

            try {
                // TODO

                return CreateResponse(requestHeader, context.StringTable);
            }
            catch (ServiceResultException e) {
                lock (_lock) {
                    ServerDiagnostics.RejectedRequestsCount++;

                    if (StatusCodeEx.IsSecurityError(e.StatusCode)) {
                        ServerDiagnostics.SecurityRejectedRequestsCount++;
                    }
                }

                throw e;
            }
            finally {
                OnRequestComplete(context);
            }
        }

        /// <inheritdoc/>
        public override ResponseHeader SetPublishingMode(
            RequestHeader requestHeader,
            bool publishingEnabled,
            UInt32Collection subscriptionIds,
            out StatusCodeCollection results,
            out DiagnosticInfoCollection diagnosticInfos) {
            var context = ValidateRequest(requestHeader, RequestType.SetPublishingMode);

            try {
                if (subscriptionIds == null || subscriptionIds.Count == 0) {
                    throw new ServiceResultException(StatusCodes.BadNothingToDo);
                }
                // TODO

                return CreateResponse(requestHeader, context.StringTable);
            }
            catch (ServiceResultException e) {
                lock (_lock) {
                    ServerDiagnostics.RejectedRequestsCount++;

                    if (StatusCodeEx.IsSecurityError(e.StatusCode)) {
                        ServerDiagnostics.SecurityRejectedRequestsCount++;
                    }
                }

                throw e;
            }
            finally {
                OnRequestComplete(context);
            }
        }

        /// <inheritdoc/>
        public override ResponseHeader SetTriggering(
            RequestHeader requestHeader,
            uint subscriptionId,
            uint triggeringItemId,
            UInt32Collection linksToAdd,
            UInt32Collection linksToRemove,
            out StatusCodeCollection addResults,
            out DiagnosticInfoCollection addDiagnosticInfos,
            out StatusCodeCollection removeResults,
            out DiagnosticInfoCollection removeDiagnosticInfos) {
            addResults = null;
            addDiagnosticInfos = null;
            removeResults = null;
            removeDiagnosticInfos = null;

            var context = ValidateRequest(requestHeader, RequestType.SetTriggering);

            try {
                if ((linksToAdd == null || linksToAdd.Count == 0) &&
                    (linksToRemove == null || linksToRemove.Count == 0)) {
                    throw new ServiceResultException(StatusCodes.BadNothingToDo);
                }

                // TODO

                return CreateResponse(requestHeader, context.StringTable);
            }
            catch (ServiceResultException e) {
                lock (_lock) {
                    ServerDiagnostics.RejectedRequestsCount++;

                    if (StatusCodeEx.IsSecurityError(e.StatusCode)) {
                        ServerDiagnostics.SecurityRejectedRequestsCount++;
                    }
                }

                throw e;
            }
            finally {
                OnRequestComplete(context);
            }
        }

        /// <inheritdoc/>
        public override ResponseHeader CreateMonitoredItems(
            RequestHeader requestHeader,
            uint subscriptionId,
            TimestampsToReturn timestampsToReturn,
            MonitoredItemCreateRequestCollection itemsToCreate,
            out MonitoredItemCreateResultCollection results,
            out DiagnosticInfoCollection diagnosticInfos) {
            var context = ValidateRequest(requestHeader, RequestType.CreateMonitoredItems);

            try {
                if (itemsToCreate == null || itemsToCreate.Count == 0) {
                    throw new ServiceResultException(StatusCodes.BadNothingToDo);
                }

                // TODO

                return CreateResponse(requestHeader, context.StringTable);
            }
            catch (ServiceResultException e) {
                lock (_lock) {
                    ServerDiagnostics.RejectedRequestsCount++;

                    if (StatusCodeEx.IsSecurityError(e.StatusCode)) {
                        ServerDiagnostics.SecurityRejectedRequestsCount++;
                    }
                }

                throw e;
            }
            finally {
                OnRequestComplete(context);
            }
        }

        /// <inheritdoc/>
        public override ResponseHeader ModifyMonitoredItems(
            RequestHeader requestHeader,
            uint subscriptionId,
            TimestampsToReturn timestampsToReturn,
            MonitoredItemModifyRequestCollection itemsToModify,
            out MonitoredItemModifyResultCollection results,
            out DiagnosticInfoCollection diagnosticInfos) {
            var context = ValidateRequest(requestHeader, RequestType.ModifyMonitoredItems);

            try {
                if (itemsToModify == null || itemsToModify.Count == 0) {
                    throw new ServiceResultException(StatusCodes.BadNothingToDo);
                }

                // TODO

                return CreateResponse(requestHeader, context.StringTable);
            }
            catch (ServiceResultException e) {
                lock (_lock) {
                    ServerDiagnostics.RejectedRequestsCount++;

                    if (StatusCodeEx.IsSecurityError(e.StatusCode)) {
                        ServerDiagnostics.SecurityRejectedRequestsCount++;
                    }
                }

                throw e;
            }
            finally {
                OnRequestComplete(context);
            }
        }

        /// <inheritdoc/>
        public override ResponseHeader DeleteMonitoredItems(
            RequestHeader requestHeader,
            uint subscriptionId,
            UInt32Collection monitoredItemIds,
            out StatusCodeCollection results,
            out DiagnosticInfoCollection diagnosticInfos) {
            var context = ValidateRequest(requestHeader, RequestType.DeleteMonitoredItems);

            try {
                if (monitoredItemIds == null || monitoredItemIds.Count == 0) {
                    throw new ServiceResultException(StatusCodes.BadNothingToDo);
                }

                // TODO

                return CreateResponse(requestHeader, context.StringTable);
            }
            catch (ServiceResultException e) {
                lock (_lock) {
                    ServerDiagnostics.RejectedRequestsCount++;

                    if (StatusCodeEx.IsSecurityError(e.StatusCode)) {
                        ServerDiagnostics.SecurityRejectedRequestsCount++;
                    }
                }

                throw e;
            }
            finally {
                OnRequestComplete(context);
            }
        }

        /// <inheritdoc/>
        public override ResponseHeader SetMonitoringMode(
            RequestHeader requestHeader,
            uint subscriptionId,
            MonitoringMode monitoringMode,
            UInt32Collection monitoredItemIds,
            out StatusCodeCollection results,
            out DiagnosticInfoCollection diagnosticInfos) {
            var context = ValidateRequest(requestHeader, RequestType.SetMonitoringMode);

            try {
                if (monitoredItemIds == null || monitoredItemIds.Count == 0) {
                    throw new ServiceResultException(StatusCodes.BadNothingToDo);
                }

                // TODO

                return CreateResponse(requestHeader, context.StringTable);
            }
            catch (ServiceResultException e) {
                lock (_lock) {
                    ServerDiagnostics.RejectedRequestsCount++;

                    if (StatusCodeEx.IsSecurityError(e.StatusCode)) {
                        ServerDiagnostics.SecurityRejectedRequestsCount++;
                    }
                }

                throw e;
            }
            finally {
                OnRequestComplete(context);
            }
        }
#endif
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
            }
            ServerError = null;
            base.Dispose(disposing);
        }

        /// <inheritdoc/>
        protected override void ValidateRequest(RequestHeader requestHeader) {
            // check for server error.
            var error = ServerError;
            if (ServiceResult.IsBad(error)) {
                throw new ServiceResultException(error);
            }
            // check server state.
            if (!ValidateIsRunning()) {
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
        private async Task<ApplicationDescriptionCollection> FindServersAsync(
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
            return new ApplicationDescriptionCollection(applications
                .Select(a => new ApplicationDescription {
                    ApplicationName = a.ApplicationName,
                    ApplicationType = a.ApplicationType.ToStackType(),
                    ApplicationUri = a.ApplicationUri,
                    DiscoveryProfileUri = a.DiscoveryProfileUri,
                    ProductUri = a.ProductUri,
                    DiscoveryUrls = new StringCollection {
                        url + "/" + a.ApplicationId  // TODO: Get from transports
                    },
                    GatewayServerUri = url + "/" + a.ApplicationId
                }));
        }

        /// <summary>
        /// Get endpoints from registry and return endpoint description collection
        /// </summary>
        /// <param name="discoveryUrl"></param>
        /// <param name="profileUris"></param>
        /// <returns></returns>
        private async Task<EndpointDescriptionCollection> GetEndpointsAsync(
            string discoveryUrl, StringCollection profileUris = null) {
            EndpointDescriptionCollection endpoints;
            var url = Utils.ParseUri(discoveryUrl);
            if (url == null || url.Segments.Length == 0) {
                endpoints = new EndpointDescriptionCollection();
            }
            else {
                var applicationId = url.Segments[url.Segments.Length - 1];
                var application = await _registry.GetApplicationAsync(applicationId);

                // TODO: Filter from transports
                var profiles = new StringCollection();
                if (profileUris == null || profileUris.Contains(Profiles.HttpsBinaryTransport)) {
                    profiles.Add(Profiles.HttpsBinaryTransport);
                }
                if (profileUris != null && profileUris.Contains(Profiles.UaTcpTransport)) {
                    profiles.Add(Profiles.UaTcpTransport);
                }

                // Make server info on the fly
                var server = new ApplicationDescription {
                    ApplicationName =
                        application.Application.ApplicationName,
                    ApplicationType =
                        application.Application.ApplicationType.ToStackType(),
                    ApplicationUri =
                        application.Application.ApplicationUri,
                    DiscoveryProfileUri =
                        application.Application.DiscoveryProfileUri,
                    DiscoveryUrls = new StringCollection {
                        url.ToString()  // TODO: Get from transports
                    },
                    GatewayServerUri =
                        url.ToString(),
                    ProductUri = application.Application.ProductUri
                };

                // Make endpoints
                endpoints = new EndpointDescriptionCollection(application.Endpoints
                    .Select(t => new EndpointDescription {
                        SecurityLevel = (byte)(t.SecurityLevel ?? 0),
                        EndpointUrl = url + "/" + t.Id, // Make twin endpoint url
                        SecurityMode = MessageSecurityMode.SignAndEncrypt,
                        SecurityPolicyUri = t.Endpoint.SecurityPolicy,

                        // Get endpoint template from each transport
                        Server = server,
                        TransportProfileUri = Profiles.HttpsBinaryTransport, // TODO
                        ServerCertificate = t.Certificate, // TODO: Need to use our certificate
                        ProxyUrl = null,
                        UserIdentityTokens = null // TODO
                    }));
            }
            return endpoints;
        }

        /// <summary>
        /// Browse on twin
        /// </summary>
        /// <param name="context"></param>
        /// <param name="requestHeader"></param>
        /// <param name="view"></param>
        /// <param name="requestedMaxReferencesPerNode"></param>
        /// <param name="nodesToBrowse"></param>
        /// <param name="results"></param>
        /// <param name="diagnosticInfos"></param>
        /// <returns></returns>
        private async Task BrowseAsync(RequestContextModel context,
            RequestHeader requestHeader, ViewDescription view,
            uint requestedMaxReferencesPerNode, BrowseDescriptionCollection nodesToBrowse,
            BrowseResultCollection results, DiagnosticInfoCollection diagnosticInfos) {
            var twin = ToTwinId(context.ChannelContext.EndpointDescription);
            for (var i = 0; i < nodesToBrowse.Count; i++) {
                try {
                    var diagnostics = requestHeader.ToServiceModel();

                    // Call service
                    var response = await _browser.NodeBrowseFirstAsync(twin,
                        new Models.BrowseRequestModel {
                            NodeId = nodesToBrowse[i].NodeId
                                .AsString(context.Session.MessageContext),
                            Diagnostics = diagnostics,
                            Direction = nodesToBrowse[i].BrowseDirection
                                .ToServiceType(),
                            MaxReferencesToReturn = requestedMaxReferencesPerNode,
                            NoSubtypes = !nodesToBrowse[i].IncludeSubtypes,
                            ReadVariableValues = false,
                            TargetNodesOnly = true,
                            ReferenceTypeId = nodesToBrowse[i].ReferenceTypeId
                                .AsString(context.Session.MessageContext),
                            View = view.ToServiceModel(
                                context.Session.MessageContext),
                            // NodeClassMask = nodesToBrowse[i].NodeClassMask,
                            // ResultMask = nodesToBrowse[i].ResultMask,
                            Elevation = null
                        });

                    // Update results
                    OperationResultEx.ToDiagnosticsInfo(response.Diagnostics,
                        diagnostics, context.Session.MessageContext,
                        out var statusCode, out var diagnosticInfo);
                    diagnosticInfos[i] = diagnosticInfo;

                    // Get references
                    var references = response.References
                        .Select(r => new ReferenceDescription {
                            BrowseName = r.BrowseName,
                            DisplayName = r.DisplayName,
                            IsForward =
                                r.Direction == Models.BrowseDirection.Forward,
                            NodeId = r.Target.Id.ToNodeId(
                                context.Session.MessageContext),
                            ReferenceTypeId = r.Id.ToNodeId(
                                context.Session.MessageContext),
                            NodeClass = NodeClass.Unspecified
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
        /// Browse next on twin
        /// </summary>
        /// <param name="context"></param>
        /// <param name="requestHeader"></param>
        /// <param name="releaseContinuationPoints"></param>
        /// <param name="continuationPoints"></param>
        /// <param name="results"></param>
        /// <param name="diagnosticInfos"></param>
        /// <returns></returns>
        private async Task BrowseNextAsync(RequestContextModel context,
            RequestHeader requestHeader, bool releaseContinuationPoints,
            ByteStringCollection continuationPoints, BrowseResultCollection results,
            DiagnosticInfoCollection diagnosticInfos) {
            var twin = ToTwinId(context.ChannelContext.EndpointDescription);
            for (var i = 0; i < continuationPoints.Count; i++) {
                try {
                    var diagnostics = requestHeader.ToServiceModel();

                    // Call service
                    var response = await _browser.NodeBrowseNextAsync(twin,
                        new Models.BrowseNextRequestModel {
                            ContinuationToken =
                                continuationPoints[i]?.ToBase64String(),
                            Diagnostics = diagnostics,
                            Abort = releaseContinuationPoints,
                            ReadVariableValues = false,
                            TargetNodesOnly = true,
                            Elevation = null
                        });

                    // Update results
                    OperationResultEx.ToDiagnosticsInfo(response.Diagnostics,
                        diagnostics, context.Session.MessageContext,
                        out var statusCode, out var diagnosticInfo);
                    diagnosticInfos[i] = diagnosticInfo;

                    // Get references
                    var references = response.References
                        .Select(r => new ReferenceDescription {
                            BrowseName = r.BrowseName,
                            DisplayName = r.DisplayName,
                            IsForward =
                                r.Direction == Models.BrowseDirection.Forward,
                            NodeId = r.Target.Id.ToNodeId(
                                context.Session.MessageContext),
                            ReferenceTypeId = r.Id.ToNodeId(
                                context.Session.MessageContext),
                            NodeClass = NodeClass.Unspecified
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
        /// Call method on twin
        /// </summary>
        /// <param name="context"></param>
        /// <param name="requestHeader"></param>
        /// <param name="methodsToCall"></param>
        /// <param name="results"></param>
        /// <param name="diagnosticInfos"></param>
        /// <returns></returns>
        private async Task CallAsync(RequestContextModel context,
            RequestHeader requestHeader, CallMethodRequestCollection methodsToCall,
            CallMethodResultCollection results, DiagnosticInfoCollection diagnosticInfos) {
            var twin = ToTwinId(context.ChannelContext.EndpointDescription);
            for (var i = 0; i < methodsToCall.Count; i++) {
                try {
                    var diagnostics = requestHeader.ToServiceModel();

                    // Convert input arguments
                    var inputs = methodsToCall[i].InputArguments?
                        .Select(v => new Models.MethodCallArgumentModel {
                            Value = _encoder.Encode(v, out var type,
                                context.Session.MessageContext),
                            DataType = type.ToString()
                        })
                        .ToList();

                    // Call service
                    var response = await _twin.NodeMethodCallAsync(twin,
                        new Models.MethodCallRequestModel {
                            MethodId = methodsToCall[i].MethodId.AsString(
                                context.Session.MessageContext),
                            ObjectId = methodsToCall[i].ObjectId.AsString(
                                context.Session.MessageContext),
                            Diagnostics = diagnostics,
                            Arguments = inputs,
                            Elevation = null
                        });

                    // Update results
                    OperationResultEx.ToDiagnosticsInfo(response.Diagnostics,
                        diagnostics, context.Session.MessageContext,
                        out var statusCode, out var diagnosticInfo);
                    diagnosticInfos[i] = diagnosticInfo;

                    // Convert output arguments
                    var outputs = response.Results?.Select(r =>
                        _encoder.Decode(r.Value, TypeInfo.GetBuiltInType(
                                r.DataType.ToNodeId(context.Session.MessageContext)),
                            context.Session.MessageContext));

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
        /// Read from twin
        /// </summary>
        /// <param name="context"></param>
        /// <param name="requestHeader"></param>
        /// <param name="maxAge"></param>
        /// <param name="nodesToRead"></param>
        /// <param name="results"></param>
        /// <param name="diagnosticInfos"></param>
        /// <returns></returns>
        private async Task ReadAsync(RequestContextModel context,
            RequestHeader requestHeader, double maxAge,
            ReadValueIdCollection nodesToRead, DataValueCollection results,
            DiagnosticInfoCollection diagnosticInfos) {
            var twin = ToTwinId(context.ChannelContext.EndpointDescription);
            for (var i = 0; i < nodesToRead.Count; i++) {
                try {
                    var diagnostics = requestHeader.ToServiceModel();

                    // Call service
                    var response = await _twin.NodeValueReadAsync(twin,
                        new Models.ValueReadRequestModel {
                            IndexRange = nodesToRead[i].IndexRange,
                            NodeId = nodesToRead[i].NodeId.AsString(
                                context.Session.MessageContext),
                            Diagnostics = diagnostics,
                            MaxAge = TimeSpan.FromMilliseconds(maxAge),
                            Elevation = null
                        });

                    // Update results
                    OperationResultEx.ToDiagnosticsInfo(response.Diagnostics,
                        diagnostics, context.Session.MessageContext,
                        out var statusCode, out var diagnosticInfo);
                    diagnosticInfos[i] = diagnosticInfo;
                    if (statusCode != StatusCodes.Good) {
                        results[i] = new DataValue(statusCode);
                    }
                    else {
                        results[i] = new DataValue(_encoder.Decode(response.Value)) {
                            ServerPicoseconds =
                                response.ServerPicoseconds ?? 0,
                            ServerTimestamp =
                                response.ServerTimestamp ?? DateTime.MinValue,
                            SourcePicoseconds =
                                response.SourcePicoseconds ?? 0,
                            SourceTimestamp =
                                response.SourceTimestamp ?? DateTime.MinValue
                        };
                    }
                }
                catch (Exception ex) {
                    diagnosticInfos[i] = new DiagnosticInfo(ex,
                        (DiagnosticsMasks)requestHeader.ReturnDiagnostics, true,
                        context.StringTable);
                    results[i] = new DataValue(StatusCodes.BadNotReadable);
                }
            }
        }

        /// <summary>
        /// Write to twin
        /// </summary>
        /// <param name="context"></param>
        /// <param name="requestHeader"></param>
        /// <param name="nodesToWrite"></param>
        /// <param name="results"></param>
        /// <param name="diagnosticInfos"></param>
        /// <returns></returns>
        private async Task WriteAsync(RequestContextModel context,
            RequestHeader requestHeader, WriteValueCollection nodesToWrite,
            StatusCodeCollection results, DiagnosticInfoCollection diagnosticInfos) {
            var twin = ToTwinId(context.ChannelContext.EndpointDescription);
            for (var i = 0; i < nodesToWrite.Count; i++) {
                try {
                    var diagnostics = requestHeader.ToServiceModel();
                    // Call service
                    var response = await _twin.NodeValueWriteAsync(twin,
                        new Models.ValueWriteRequestModel {
                            IndexRange = nodesToWrite[i].IndexRange,
                            NodeId = nodesToWrite[i].NodeId.AsString(
                                context.Session.MessageContext),
                            Diagnostics = diagnostics,
                            DataType = nodesToWrite[i].TypeId.AsString(
                                context.Session.MessageContext),
                            Value = _encoder.Encode(
                                nodesToWrite[i].Value.WrappedValue, out var type,
                                context.Session.MessageContext),
                            Elevation = null
                        });

                    // Update results
                    OperationResultEx.ToDiagnosticsInfo(response.Diagnostics,
                        diagnostics, context.Session.MessageContext,
                        out var statusCode, out var diagnosticInfo);
                    diagnosticInfos[i] = diagnosticInfo;
                    results[i] = statusCode;
                }
                catch (Exception ex) {
                    diagnosticInfos[i] = new DiagnosticInfo(ex,
                        (DiagnosticsMasks)requestHeader.ReturnDiagnostics, true,
                        context.StringTable);
                    results[i] = StatusCodes.BadNotWritable;
                }
            }
        }

        /// <summary>
        /// Parse the twin id out of the endpoint description.  The endpoint url is
        /// assumed to end in the twin identifier.  This is what we use.
        /// </summary>
        /// <param name="endpointDescription"></param>
        /// <returns></returns>
        private string ToTwinId(EndpointDescription endpointDescription) {
            if (endpointDescription == null) {
                throw new ArgumentNullException(nameof(endpointDescription));
            }
            if (string.IsNullOrEmpty(endpointDescription.EndpointUrl)) {
                throw new ArgumentNullException(nameof(endpointDescription.EndpointUrl));
            }
            var path = Utils.ParseUri(endpointDescription.EndpointUrl).Segments;
            if (path.Length == 0) {
                throw new ArgumentException(nameof(endpointDescription.EndpointUrl));
            }
            return path[path.Length - 1];
        }

        /// <summary>
        /// Begins a request
        /// </summary>
        /// <param name="requestHeader">The request header.</param>
        /// <param name="requestType">Type of the request.</param>
        /// <returns></returns>
        protected RequestContextModel OnRequestBegin(RequestHeader requestHeader,
            RequestType requestType) {
            base.ValidateRequest(requestHeader);
            if (!ValidateIsRunning()) {
                throw new ServiceResultException(StatusCodes.BadServerHalted);
            }

            // Get operation context from session manager
            var context = SessionServices.GetContext(requestHeader, requestType);
            _logger.Debug($"{context.RequestType} {context.RequestId} validated.");

            // Pass to request manager
            _requestManager.RequestReceived(context);
            return context;
        }

        /// <summary>
        /// Completes the request
        /// </summary>
        /// <param name="context">The operation context.</param>
        protected virtual void OnRequestComplete(RequestContextModel context) {
            lock (_lock) {
                if (_requestManager == null) {
                    throw new ServiceResultException(StatusCodes.BadServerHalted);
                }
                _requestManager.RequestCompleted(context);
            }
        }

        /// <summary>
        /// Returns whether the server is running or throws if the server is in
        /// an exception condition
        /// </summary>
        /// <returns></returns>
        private bool ValidateIsRunning() {
            lock (_lock) {
                if (ServiceResult.IsBad(ServerError)) {
                    throw new ServiceResultException(ServerError);
                }
                if (_requestManager == null) {
                    throw new ServiceResultException(StatusCodes.BadServerHalted);
                }
                return _serverState == ServerState.Running;
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
        /// <param name="applicationUri"></param>
        /// <param name="clientCertificate"></param>
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

        private readonly object _lock = new object();
        private readonly IApplicationRegistry _registry;
        private readonly IVariantEncoder _encoder;
        private readonly ILogger _logger;
        private readonly IBrowseServices<string> _browser;
        private readonly INodeServices<string> _twin;

        private RequestManager _requestManager;
        private ApplicationConfiguration _configuration;
        private ServerState _serverState;





















        /// <summary>
        /// Create a new service host for UA TCP.
        /// </summary>
        protected List<EndpointDescription> CreateUaTcpServiceHostEx(
            IDictionary<string, Task> hosts,
            ApplicationConfiguration configuration,
            IList<string> baseAddresses,
            ApplicationDescription serverDescription,
            List<ServerSecurityPolicy> securityPolicies) {
            // generate a unique host name.
            var hostName = string.Empty;

            if (hosts.ContainsKey(hostName)) {
                hostName = "/Tcp";
            }

            if (hosts.ContainsKey(hostName)) {
                hostName += Utils.Format("/{0}", hosts.Count);
            }

            // build list of uris.
            var uris = new List<Uri>();
            var endpoints = new EndpointDescriptionCollection();

            // create the endpoint configuration to use.
            var endpointConfiguration = EndpointConfiguration.Create(configuration);
            var computerName = Utils.GetHostName();

            for (var ii = 0; ii < baseAddresses.Count; ii++) {
                // UA TCP and HTTPS endpoints support multiple policies.
                if (!baseAddresses[ii].StartsWith(Utils.UriSchemeOpcTcp, StringComparison.Ordinal)) {
                    continue;
                }

                var uri = new UriBuilder(baseAddresses[ii]);

                if (string.Compare(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase) == 0) {
                    uri.Host = computerName;
                }

                uris.Add(uri.Uri);

                foreach (var policy in securityPolicies) {
                    // create the endpoint description.
                    var description = new EndpointDescription();

                    description.EndpointUrl = uri.ToString();
                    description.Server = serverDescription;

                    description.SecurityMode = policy.SecurityMode;
                    description.SecurityPolicyUri = policy.SecurityPolicyUri;
                    description.SecurityLevel = ServerSecurityPolicy.CalculateSecurityLevel(policy.SecurityMode, policy.SecurityPolicyUri);
                    description.UserIdentityTokens = GetUserTokenPolicies(configuration, description);
                    description.TransportProfileUri = Profiles.UaTcpTransport;

                    var requireEncryption = RequireEncryption(description);

                    if (!requireEncryption) {
                        foreach (var userTokenPolicy in description.UserIdentityTokens) {
                            if (userTokenPolicy.SecurityPolicyUri != SecurityPolicies.None) {
                                requireEncryption = true;
                                break;
                            }
                        }
                    }

                    if (requireEncryption) {
                        description.ServerCertificate = InstanceCertificate.RawData;

                        // check if complete chain should be sent.
                        if (configuration.SecurityConfiguration.SendCertificateChain && InstanceCertificateChain != null && InstanceCertificateChain.Count > 0) {
                            var serverCertificateChain = new List<byte>();

                            for (var i = 0; i < InstanceCertificateChain.Count; i++) {
                                serverCertificateChain.AddRange(InstanceCertificateChain[i].RawData);
                            }

                            description.ServerCertificate = serverCertificateChain.ToArray();
                        }
                    }

                    endpoints.Add(description);
                }

                // create the UA-TCP stack listener.
                try {
                    var settings = new TransportListenerSettings();

                    settings.Descriptions = endpoints;
                    settings.Configuration = endpointConfiguration;
                    settings.CertificateValidator = configuration.CertificateValidator.GetChannelValidator();
                    settings.NamespaceUris = this.MessageContext.NamespaceUris;
                    settings.Factory = this.MessageContext.Factory;
                    settings.ServerCertificate = this.InstanceCertificate;

                    if (configuration.SecurityConfiguration.SendCertificateChain) {
                        settings.ServerCertificateChain = this.InstanceCertificateChain;
                    }

                    ITransportListener listener = new Opc.Ua.Bindings.UaTcpChannelListener();

                    listener.Open(
                       uri.Uri,
                       settings,
                       GetEndpointInstance(this));

                    TransportListeners.Add(listener);
                }
                catch (Exception e) {
                    Utils.Trace(e, "Could not load UA-TCP Stack Listener.");
                    throw e;
                }
            }

            return endpoints;
        }

        /// <inheritdoc/>
        protected List<EndpointDescription> CreateHttpsServiceHostEx(
            IDictionary<string, Task> hosts,
            ApplicationConfiguration configuration,
            IList<string> baseAddresses,
            ApplicationDescription serverDescription,
            List<ServerSecurityPolicy> securityPolicies) {
            // generate a unique host name.
            var hostName = string.Empty;

            if (hosts.ContainsKey(hostName)) {
                hostName = "/Https";
            }

            if (hosts.ContainsKey(hostName)) {
                hostName += Utils.Format("/{0}", hosts.Count);
            }

            // build list of uris.
            var uris = new List<Uri>();
            var endpoints = new EndpointDescriptionCollection();

            // create the endpoint configuration to use.
            var endpointConfiguration = EndpointConfiguration.Create(configuration);
            var computerName = Utils.GetHostName();

            for (var ii = 0; ii < baseAddresses.Count; ii++) {
                if (!baseAddresses[ii].StartsWith(Utils.UriSchemeHttps, StringComparison.Ordinal)) {
                    continue;
                }

                var uri = new UriBuilder(baseAddresses[ii]);

                if (uri.Path[uri.Path.Length - 1] != '/') {
                    uri.Path += "/";
                }

                if (string.Compare(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase) == 0) {
                    uri.Host = computerName;
                }

                uris.Add(uri.Uri);

                if (uri.Scheme == Utils.UriSchemeHttps) {
                    // Can only support one policy with HTTPS so pick the
                    // first secure policy with sign and encrypt in the list
                    ServerSecurityPolicy bestPolicy = null;
                    foreach (var policy in securityPolicies) {
                        if (policy.SecurityMode != MessageSecurityMode.SignAndEncrypt) {
                            continue;
                        }

                        bestPolicy = policy;
                        break;
                    }

                    if (bestPolicy == null) {
                        throw new ServiceResultException("HTTPS transport requires policy with sign and encrypt.");
                    }

                    var description = new EndpointDescription();

                    description.EndpointUrl = uri.ToString();
                    description.Server = serverDescription;

                    if (InstanceCertificate != null) {
                        description.ServerCertificate = InstanceCertificate.RawData;

                        // check if complete chain should be sent.
                        if (configuration.SecurityConfiguration.SendCertificateChain && InstanceCertificateChain != null && InstanceCertificateChain.Count > 0) {
                            var serverCertificateChain = new List<byte>();

                            for (var i = 0; i < InstanceCertificateChain.Count; i++) {
                                serverCertificateChain.AddRange(InstanceCertificateChain[i].RawData);
                            }

                            description.ServerCertificate = serverCertificateChain.ToArray();
                        }
                    }

                    description.SecurityMode = bestPolicy.SecurityMode;
                    description.SecurityPolicyUri = bestPolicy.SecurityPolicyUri;
                    description.SecurityLevel = ServerSecurityPolicy.CalculateSecurityLevel(bestPolicy.SecurityMode, bestPolicy.SecurityPolicyUri);
                    description.UserIdentityTokens = GetUserTokenPolicies(configuration, description);
                    description.TransportProfileUri = Profiles.HttpsBinaryTransport;

                    endpoints.Add(description);
                }

                // create the stack listener.
                try {
                    var settings = new TransportListenerSettings();

                    settings.Descriptions = endpoints;
                    settings.Configuration = endpointConfiguration;
                    settings.ServerCertificate = this.InstanceCertificate;
                    settings.CertificateValidator = configuration.CertificateValidator.GetChannelValidator();
                    settings.NamespaceUris = this.MessageContext.NamespaceUris;
                    settings.Factory = this.MessageContext.Factory;

                    ITransportListener listener = new Opc.Ua.Bindings.UaHttpsChannelListener();

                    listener.Open(
                       uri.Uri,
                       settings,
                       GetEndpointInstance(this));

                    TransportListeners.Add(listener);
                }
                catch (Exception e) {
                    var message = "Could not load HTTPS Stack Listener.";
                    if (e.InnerException != null) {
                        message += (" " + e.InnerException.Message);
                    }
                    Utils.Trace(e, message);
                }
            }

            return endpoints;
        }

        /// <inheritdoc/>
        protected override UserTokenPolicyCollection GetUserTokenPolicies(
            ApplicationConfiguration configuration, EndpointDescription description) {
            var policies = new UserTokenPolicyCollection();

            if (configuration.ServerConfiguration == null ||
                configuration.ServerConfiguration.UserTokenPolicies == null) {
                return policies;
            }

            foreach (var policy in configuration.ServerConfiguration.UserTokenPolicies) {
                // ensure a security policy is specified for user tokens.
                if (description.SecurityMode == MessageSecurityMode.None) {
                    if (string.IsNullOrEmpty(policy.SecurityPolicyUri)) {
                        var clone = (UserTokenPolicy)policy.MemberwiseClone();
                        clone.SecurityPolicyUri = SecurityPolicies.Basic256;
                        policies.Add(clone);
                        continue;
                    }
                }

                policies.Add(policy);
            }

            // ensure each policy has a unique id.
            for (var ii = 0; ii < policies.Count; ii++) {
                if (string.IsNullOrEmpty(policies[ii].PolicyId)) {
                    policies[ii].PolicyId = Utils.Format("{0}", ii);
                }
            }

            return policies;
        }
















        /// <inheritdoc/>
        protected override IList<Task> InitializeServiceHosts(
            ApplicationConfiguration configuration,
            out ApplicationDescription serverDescription,
            out EndpointDescriptionCollection endpoints) {
            serverDescription = null;
            endpoints = null;

            var hosts = new Dictionary<string, Task>();

            // ensure at least one security policy exists.
            if (configuration.ServerConfiguration.SecurityPolicies.Count == 0) {
                configuration.ServerConfiguration.SecurityPolicies.Add(new ServerSecurityPolicy());
            }

            // ensure at least one user token policy exists.
            if (configuration.ServerConfiguration.UserTokenPolicies.Count == 0) {
                var userTokenPolicy = new UserTokenPolicy {
                    TokenType = UserTokenType.Anonymous
                };
                userTokenPolicy.PolicyId = userTokenPolicy.TokenType.ToString();
                configuration.ServerConfiguration.UserTokenPolicies.Add(userTokenPolicy);
            }

            // set server description.
            serverDescription = new ApplicationDescription {
                ApplicationUri = configuration.ApplicationUri,
                ApplicationName = new LocalizedText("en-US", configuration.ApplicationName),
                ApplicationType = configuration.ApplicationType,
                ProductUri = configuration.ProductUri,
                DiscoveryUrls = GetDiscoveryUrls()
            };

            endpoints = new EndpointDescriptionCollection();
            IList<EndpointDescription> endpointsForHost = null;

            // create UA TCP host.
            endpointsForHost = CreateUaTcpServiceHost(
                hosts,
                configuration,
                configuration.ServerConfiguration.BaseAddresses,
                serverDescription,
                configuration.ServerConfiguration.SecurityPolicies);

            endpoints.InsertRange(0, endpointsForHost);

            // create HTTPS host.
            endpointsForHost = CreateHttpsServiceHost(
                hosts,
                configuration,
                configuration.ServerConfiguration.BaseAddresses,
                serverDescription,
                configuration.ServerConfiguration.SecurityPolicies);

            endpoints.AddRange(endpointsForHost);
            return new List<Task>(hosts.Values);
        }

        /// <inheritdoc/>
        protected override EndpointBase GetEndpointInstance(ServerBase server) {
            return new SessionEndpoint(server);
        }

        /// <inheritdoc/>
        protected override void StartApplication(ApplicationConfiguration configuration) {
            base.StartApplication(configuration);
            try {
                lock (_lock) {
                    _configuration = configuration;

                    _requestManager = new RequestManager(null);
                    SessionServices = new SessionServices(_configuration, _logger);

                    // Register for diagnostics
                    SessionServices.SessionClosing += (s, _) => {
                        lock (_lock) {
                            ServerDiagnostics.CurrentSessionCount--;
                        }
                    };
                    SessionServices.SessionCreated += (s, _) => {
                        lock (_lock) {
                            ServerDiagnostics.CurrentSessionCount++;
                            ServerDiagnostics.CumulatedSessionCount++;
                        }
                    };
                    SessionServices.SessionTimeout += (s, _) => {
                        lock (_lock) {
                            ServerDiagnostics.SessionTimeoutCount++;
                        }
                    };

                    // set the server status as running.
                    ServerError = null;
                    _serverState = ServerState.Running;
                }
            }
            catch (Exception e) {
                OnServerStopping();
                var error = ServiceResult.Create(e, StatusCodes.BadInternalError,
                    "Unexpected error starting application");
                ServerError = error;
                throw new ServiceResultException(error);
            }
        }

        /// <inheritdoc/>
        protected override void OnServerStopping() {
            // attempt graceful shutdown the server.
            if (SessionServices != null || _requestManager != null) {
                lock (_lock) {
                    _serverState = ServerState.Shutdown;

                    Utils.SilentDispose(_requestManager);
                    _requestManager = null;
                    Utils.SilentDispose(SessionServices);
                    SessionServices = null;
                }
            }
        }
    }
}
