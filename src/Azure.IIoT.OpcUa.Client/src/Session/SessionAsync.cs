/* ========================================================================
 * Copyright (c) 2005-2020 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

namespace Opc.Ua.Client
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using Opc.Ua.Bindings;

    /// <summary>
    /// Manages a session with a server.
    /// Contains the async versions of the public session api.
    /// </summary>
    public partial class Session : SessionClientBatched, ISession
    {
        /// <inheritdoc/>
        public Task OpenAsync(
            string sessionName,
            IUserIdentity identity,
            CancellationToken ct)
        {
            return OpenAsync(sessionName, 0, identity, null, ct);
        }

        /// <inheritdoc/>
        public Task OpenAsync(
            string sessionName,
            uint sessionTimeout,
            IUserIdentity identity,
            IList<string> preferredLocales,
            CancellationToken ct)
        {
            return OpenAsync(sessionName, sessionTimeout, identity, preferredLocales, true, ct);
        }

        /// <inheritdoc/>
        public async Task OpenAsync(
            string sessionName,
            uint sessionTimeout,
            IUserIdentity identity,
            IList<string> preferredLocales,
            bool checkDomain,
            CancellationToken ct)
        {
            OpenValidateIdentity(ref identity, out var identityToken, out var identityPolicy, out var securityPolicyUri, out var requireEncryption);

            // validate the server certificate /certificate chain.
            X509Certificate2 serverCertificate = null;
            var certificateData = m_endpoint.Description.ServerCertificate;

            if (certificateData?.Length > 0)
            {
                var serverCertificateChain = Utils.ParseCertificateChainBlob(certificateData);

                if (serverCertificateChain.Count > 0)
                {
                    serverCertificate = serverCertificateChain[0];
                }

                if (requireEncryption)
                {
                    // validation skipped until IOP isses are resolved.
                    // ValidateServerCertificateApplicationUri(serverCertificate);
                    if (checkDomain)
                    {
                        await m_configuration.CertificateValidator.ValidateAsync(serverCertificateChain, m_endpoint, ct).ConfigureAwait(false);
                    }
                    else
                    {
                        await m_configuration.CertificateValidator.ValidateAsync(serverCertificateChain, ct).ConfigureAwait(false);
                    }
                    // save for reconnect
                    m_checkDomain = checkDomain;
                }
            }

            // create a nonce.
            var length = (uint)m_configuration.SecurityConfiguration.NonceLength;
            var clientNonce = Utils.Nonce.CreateNonce(length);

            // send the application instance certificate for the client.
            BuildCertificateData(out var clientCertificateData, out var clientCertificateChainData);

            var clientDescription = new ApplicationDescription
            {
                ApplicationUri = m_configuration.ApplicationUri,
                ApplicationName = m_configuration.ApplicationName,
                ApplicationType = ApplicationType.Client,
                ProductUri = m_configuration.ProductUri
            };

            if (sessionTimeout == 0)
            {
                sessionTimeout = (uint)m_configuration.ClientConfiguration.DefaultSessionTimeout;
            }

            var successCreateSession = false;
            CreateSessionResponse response = null;

            //if security none, first try to connect without certificate
            if (m_endpoint.Description.SecurityPolicyUri == SecurityPolicies.None)
            {
                //first try to connect with client certificate NULL
                try
                {
                    response = await base.CreateSessionAsync(
                        null,
                        clientDescription,
                        m_endpoint.Description.Server.ApplicationUri,
                        m_endpoint.EndpointUrl.ToString(),
                        sessionName,
                        clientNonce,
                        null,
                        sessionTimeout,
                        (uint)MessageContext.MaxMessageSize,
                        ct).ConfigureAwait(false);

                    successCreateSession = true;
                }
                catch (Exception ex)
                {
                    Utils.LogInfo("Create session failed with client certificate NULL. " + ex.Message);
                    successCreateSession = false;
                }
            }

            if (!successCreateSession)
            {
                response = await base.CreateSessionAsync(
                        null,
                        clientDescription,
                        m_endpoint.Description.Server.ApplicationUri,
                        m_endpoint.EndpointUrl.ToString(),
                        sessionName,
                        clientNonce,
                        clientCertificateChainData ?? clientCertificateData,
                        sessionTimeout,
                        (uint)MessageContext.MaxMessageSize,
                        ct).ConfigureAwait(false);
            }

            var sessionId = response.SessionId;
            var sessionCookie = response.AuthenticationToken;
            var serverNonce = response.ServerNonce;
            var serverCertificateData = response.ServerCertificate;
            var serverSignature = response.ServerSignature;
            var serverEndpoints = response.ServerEndpoints;
            var serverSoftwareCertificates = response.ServerSoftwareCertificates;

            m_sessionTimeout = response.RevisedSessionTimeout;
            m_maxRequestMessageSize = response.MaxRequestMessageSize;

            // save session id.
            lock (SyncRoot)
            {
                // save session id and cookie in base
                base.SessionCreated(sessionId, sessionCookie);
            }

            Utils.LogInfo("Revised session timeout value: {0}. ", m_sessionTimeout);
            Utils.LogInfo("Max response message size value: {0}. Max request message size: {1} ",
                MessageContext.MaxMessageSize, m_maxRequestMessageSize);

            //we need to call CloseSession if CreateSession was successful but some other exception is thrown
            try
            {
                // verify that the server returned the same instance certificate.
                ValidateServerCertificateData(serverCertificateData);

                ValidateServerEndpoints(serverEndpoints);

                ValidateServerSignature(serverCertificate, serverSignature, clientCertificateData, clientCertificateChainData, clientNonce);

                HandleSignedSoftwareCertificates(serverSoftwareCertificates);

                // create the client signature.
                var dataToSign = Utils.Append(serverCertificate?.RawData, serverNonce);
                var clientSignature = SecurityPolicies.Sign(m_instanceCertificate, securityPolicyUri, dataToSign);

                // select the security policy for the user token.
                securityPolicyUri = identityPolicy.SecurityPolicyUri;

                if (String.IsNullOrEmpty(securityPolicyUri))
                {
                    securityPolicyUri = m_endpoint.Description.SecurityPolicyUri;
                }

                // save previous nonce
                var previousServerNonce = GetCurrentTokenServerNonce();

                // validate server nonce and security parameters for user identity.
                ValidateServerNonce(
                    identity,
                    serverNonce,
                    securityPolicyUri,
                    previousServerNonce,
                    m_endpoint.Description.SecurityMode);

                // sign data with user token.
                var userTokenSignature = identityToken.Sign(dataToSign, securityPolicyUri);

                // encrypt token.
                identityToken.Encrypt(serverCertificate, serverNonce, securityPolicyUri);

                // send the software certificates assigned to the client.
                var clientSoftwareCertificates = GetSoftwareCertificates();

                // copy the preferred locales if provided.
                if (preferredLocales?.Count > 0)
                {
                    m_preferredLocales = new StringCollection(preferredLocales);
                }

                // activate session.
                var activateResponse = await ActivateSessionAsync(
                    null,
                    clientSignature,
                    clientSoftwareCertificates,
                    m_preferredLocales,
                    new ExtensionObject(identityToken),
                    userTokenSignature,
                    ct).ConfigureAwait(false);

                serverNonce = activateResponse.ServerNonce;
                var certificateResults = activateResponse.Results;
                var certificateDiagnosticInfos = activateResponse.DiagnosticInfos;

                if (certificateResults != null)
                {
                    for (var i = 0; i < certificateResults.Count; i++)
                    {
                        Utils.LogInfo("ActivateSession result[{0}] = {1}", i, certificateResults[i]);
                    }
                }

                if (clientSoftwareCertificates?.Count > 0 && (certificateResults == null || certificateResults.Count == 0))
                {
                    Utils.LogInfo("Empty results were received for the ActivateSession call.");
                }

                // fetch namespaces.
                await FetchNamespaceTablesAsync(ct).ConfigureAwait(false);

                lock (SyncRoot)
                {
                    // save nonces.
                    m_sessionName = sessionName;
                    m_identity = identity;
                    m_previousServerNonce = previousServerNonce;
                    m_serverNonce = serverNonce;
                    m_serverCertificate = serverCertificate;

                    // update system context.
                    m_systemContext.PreferredLocales = m_preferredLocales;
                    m_systemContext.SessionId = this.SessionId;
                    m_systemContext.UserIdentity = identity;
                }

                // fetch operation limits
                await FetchOperationLimitsAsync(ct).ConfigureAwait(false);

                // start keep alive thread.
                StartKeepAliveTimer();

                // raise event that session configuration changed.
                IndicateSessionConfigurationChanged();

                // call session created callback, which was already set in base class only.
                SessionCreated(sessionId, sessionCookie);
            }
            catch (Exception)
            {
                try
                {
                    await base.CloseSessionAsync(null, false, CancellationToken.None).ConfigureAwait(false);
                    await CloseChannelAsync(CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Utils.LogError("Cleanup: CloseSessionAsync() or CloseChannelAsync() raised exception. " + e.Message);
                }
                finally
                {
                    SessionCreated(null, null);
                }

                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> RemoveSubscriptionAsync(Subscription subscription, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(subscription);

            if (subscription.Created)
            {
                await subscription.DeleteAsync(false, ct).ConfigureAwait(false);
            }

            lock (SyncRoot)
            {
                if (!m_subscriptions.Remove(subscription))
                {
                    return false;
                }

                subscription.Session = null;
            }

            m_SubscriptionsChanged?.Invoke(this, null);

            return true;
        }

        /// <inheritdoc/>
        public async Task<bool> RemoveSubscriptionsAsync(IEnumerable<Subscription> subscriptions, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(subscriptions);

            var subscriptionsToDelete = new List<Subscription>();

            var removed = PrepareSubscriptionsToDelete(subscriptions, subscriptionsToDelete);

            foreach (var subscription in subscriptionsToDelete)
            {
                await subscription.DeleteAsync(true, ct).ConfigureAwait(false);
            }

            if (removed)
            {
                m_SubscriptionsChanged?.Invoke(this, null);
            }

            return removed;
        }

        /// <inheritdoc/>
        public async Task<bool> ReactivateSubscriptionsAsync(
            SubscriptionCollection subscriptions,
            bool sendInitialValues,
            CancellationToken ct = default)
        {
            var subscriptionIds = CreateSubscriptionIdsForTransfer(subscriptions);
            var failedSubscriptions = 0;

            if (subscriptionIds.Count > 0)
            {
                var reconnecting = false;
                await m_reconnectLock.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    reconnecting = m_reconnecting;
                    m_reconnecting = true;

                    for (var ii = 0; ii < subscriptions.Count; ii++)
                    {
                        if (!await subscriptions[ii].TransferAsync(this, subscriptionIds[ii], new UInt32Collection(), ct).ConfigureAwait(false))
                        {
                            Utils.LogError("SubscriptionId {0} failed to reactivate.", subscriptionIds[ii]);
                            failedSubscriptions++;
                        }
                    }

                    if (sendInitialValues)
                    {
                        (var success, var resendResults) = await ResendDataAsync(subscriptions, ct).ConfigureAwait(false);
                        if (!success)
                        {
                            Utils.LogError("Failed to call resend data for subscriptions.");
                        }
                        else if (resendResults != null)
                        {
                            for (var ii = 0; ii < resendResults.Count; ii++)
                            {
                                // no need to try for subscriptions which do not exist
                                if (StatusCode.IsNotGood(resendResults[ii].StatusCode))
                                {
                                    Utils.LogError("SubscriptionId {0} failed to resend data.", subscriptionIds[ii]);
                                }
                            }
                        }
                    }

                    Utils.LogInfo("Session REACTIVATE of {0} subscriptions completed. {1} failed.", subscriptions.Count, failedSubscriptions);
                }
                finally
                {
                    m_reconnecting = reconnecting;
                    m_reconnectLock.Release();
                }

                StartPublishing(OperationTimeout, true);
            }
            else
            {
                Utils.LogInfo("No subscriptions. TransferSubscription skipped.");
            }

            return failedSubscriptions == 0;
        }

        /// <inheritdoc/>
        public async Task<(bool, IList<ServiceResult>)> ResendDataAsync(IEnumerable<Subscription> subscriptions, CancellationToken ct)
        {
            var requests = CreateCallRequestsForResendData(subscriptions);

            var errors = new List<ServiceResult>(requests.Count);
            try
            {
                var response = await CallAsync(null, requests, ct).ConfigureAwait(false);
                var results = response.Results;
                var diagnosticInfos = response.DiagnosticInfos;
                var responseHeader = response.ResponseHeader;
                ClientBase.ValidateResponse(results, requests);
                ClientBase.ValidateDiagnosticInfos(diagnosticInfos, requests);

                var ii = 0;
                foreach (var value in results)
                {
                    var result = ServiceResult.Good;
                    if (StatusCode.IsNotGood(value.StatusCode))
                    {
                        result = ClientBase.GetResult(value.StatusCode, ii, diagnosticInfos, responseHeader);
                    }
                    errors.Add(result);
                    ii++;
                }

                return (true, errors);
            }
            catch (ServiceResultException sre)
            {
                Utils.LogError(sre, "Failed to call ResendData on server.");
            }

            return (false, errors);
        }

        /// <inheritdoc/>
        public async Task<bool> TransferSubscriptionsAsync(
            SubscriptionCollection subscriptions,
            bool sendInitialValues,
            CancellationToken ct)
        {
            var subscriptionIds = CreateSubscriptionIdsForTransfer(subscriptions);
            var failedSubscriptions = 0;

            if (subscriptionIds.Count > 0)
            {
                var reconnecting = false;
                await m_reconnectLock.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    reconnecting = m_reconnecting;
                    m_reconnecting = true;

                    var response = await base.TransferSubscriptionsAsync(null, subscriptionIds, sendInitialValues, ct).ConfigureAwait(false);
                    var results = response.Results;
                    var diagnosticInfos = response.DiagnosticInfos;
                    var responseHeader = response.ResponseHeader;

                    if (!StatusCode.IsGood(responseHeader.ServiceResult))
                    {
                        Utils.LogError("TransferSubscription failed: {0}", responseHeader.ServiceResult);
                        return false;
                    }

                    ClientBase.ValidateResponse(results, subscriptionIds);
                    ClientBase.ValidateDiagnosticInfos(diagnosticInfos, subscriptionIds);

                    for (var ii = 0; ii < subscriptions.Count; ii++)
                    {
                        if (StatusCode.IsGood(results[ii].StatusCode))
                        {
                            if (await subscriptions[ii].TransferAsync(this, subscriptionIds[ii], results[ii].AvailableSequenceNumbers, ct).ConfigureAwait(false))
                            {
                                lock (m_acknowledgementsToSendLock)
                                {
                                    // create ack for available sequence numbers
                                    foreach (var sequenceNumber in results[ii].AvailableSequenceNumbers)
                                    {
                                        AddAcknowledgementToSend(m_acknowledgementsToSend, subscriptionIds[ii], sequenceNumber);
                                    }
                                }
                            }
                        }
                        else if (results[ii].StatusCode == StatusCodes.BadNothingToDo)
                        {
                            Utils.LogInfo("SubscriptionId {0} is already member of the session.", subscriptionIds[ii]);
                            failedSubscriptions++;
                        }
                        else
                        {
                            Utils.LogError("SubscriptionId {0} failed to transfer, StatusCode={1}", subscriptionIds[ii], results[ii].StatusCode);
                            failedSubscriptions++;
                        }
                    }

                    Utils.LogInfo("Session TRANSFER ASYNC of {0} subscriptions completed. {1} failed.", subscriptions.Count, failedSubscriptions);
                }
                finally
                {
                    m_reconnecting = reconnecting;
                    m_reconnectLock.Release();
                }

                StartPublishing(OperationTimeout, false);
            }
            else
            {
                Utils.LogInfo("No subscriptions. TransferSubscription skipped.");
            }

            return failedSubscriptions == 0;
        }

        /// <inheritdoc/>
        public async Task FetchNamespaceTablesAsync(CancellationToken ct = default)
        {
            var nodesToRead = PrepareNamespaceTableNodesToRead();

            // read from server.
            var response = await ReadAsync(
                null,
                0,
                TimestampsToReturn.Neither,
                nodesToRead,
                ct).ConfigureAwait(false);

            var values = response.Results;
            var diagnosticInfos = response.DiagnosticInfos;
            var responseHeader = response.ResponseHeader;

            ValidateResponse(values, nodesToRead);
            ValidateDiagnosticInfos(diagnosticInfos, nodesToRead);

            UpdateNamespaceTable(values, diagnosticInfos, responseHeader);
        }

        /// <inheritdoc/>
        public async Task FetchTypeTreeAsync(ExpandedNodeId typeId, CancellationToken ct = default)
        {
            if (await NodeCache.FindAsync(typeId, ct).ConfigureAwait(false) is Node node)
            {
                var subTypes = new ExpandedNodeIdCollection();
                foreach (var reference in node.Find(ReferenceTypeIds.HasSubtype, false))
                {
                    subTypes.Add(reference.TargetId);
                }
                if (subTypes.Count > 0)
                {
                    await FetchTypeTreeAsync(subTypes, ct).ConfigureAwait(false);
                }
            }
        }

        /// <inheritdoc/>
        public async Task FetchTypeTreeAsync(ExpandedNodeIdCollection typeIds, CancellationToken ct = default)
        {
            var referenceTypeIds = new NodeIdCollection() { ReferenceTypeIds.HasSubtype };
            var nodes = await NodeCache.FindReferencesAsync(typeIds, referenceTypeIds, false, false, ct).ConfigureAwait(false);
            var subTypes = new ExpandedNodeIdCollection();
            foreach (var inode in nodes)
            {
                if (inode is Node node)
                {
                    foreach (var reference in node.Find(ReferenceTypeIds.HasSubtype, false))
                    {
                        if (!typeIds.Contains(reference.TargetId))
                        {
                            subTypes.Add(reference.TargetId);
                        }
                    }
                }
            }
            if (subTypes.Count > 0)
            {
                await FetchTypeTreeAsync(subTypes, ct).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Fetch the operation limits of the server.
        /// </summary>
        /// <param name="ct"></param>
        public async Task FetchOperationLimitsAsync(CancellationToken ct)
        {
            try
            {
                var operationLimitsProperties = typeof(OperationLimits)
                    .GetProperties().Select(p => p.Name).ToList();

                var nodeIds = new NodeIdCollection(
                    operationLimitsProperties.Select(name => (NodeId)typeof(VariableIds)
                    .GetField("Server_ServerCapabilities_OperationLimits_" + name, BindingFlags.Public | BindingFlags.Static)
                    .GetValue(null))
                    );

                // add the server capability MaxContinuationPointPerBrowse. Add further capabilities
                // later (when support form them will be implemented and in a more generic fashion)
                nodeIds.Add(VariableIds.Server_ServerCapabilities_MaxBrowseContinuationPoints);
                var maxBrowseContinuationPointIndex = nodeIds.Count - 1;

                (var values, var errors) = await ReadValuesAsync(nodeIds, ct).ConfigureAwait(false);

                var configOperationLimits = m_configuration?.ClientConfiguration?.OperationLimits ?? new OperationLimits();
                var operationLimits = new OperationLimits();

                for (var ii = 0; ii < operationLimitsProperties.Count; ii++)
                {
                    var property = typeof(OperationLimits).GetProperty(operationLimitsProperties[ii]);
                    var value = (uint)property.GetValue(configOperationLimits);
                    if (values[ii] != null &&
                        ServiceResult.IsNotBad(errors[ii]) && values[ii].Value is uint serverValue)
                    {
                        if (serverValue > 0 &&
                           (value == 0 || serverValue < value))
                        {
                            value = serverValue;
                        }
                    }
                    property.SetValue(operationLimits, value);
                }

                OperationLimits = operationLimits;
                if (values[maxBrowseContinuationPointIndex].Value != null
                    && ServiceResult.IsNotBad(errors[maxBrowseContinuationPointIndex]))
                {
                    ServerMaxContinuationPointsPerBrowse = (UInt16)values[maxBrowseContinuationPointIndex].Value;
                }
            }
            catch (Exception ex)
            {
                Utils.LogError(ex, "Failed to read operation limits from server. Using configuration defaults.");
                var operationLimits = m_configuration?.ClientConfiguration?.OperationLimits;
                if (operationLimits != null)
                {
                    OperationLimits = operationLimits;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<(IList<Node>, IList<ServiceResult>)> ReadNodesAsync(
            IList<NodeId> nodeIds,
            NodeClass nodeClass,
            bool optionalAttributes = false,
            CancellationToken ct = default)
        {
            if (nodeIds.Count == 0)
            {
                return (new List<Node>(), new List<ServiceResult>());
            }

            if (nodeClass == NodeClass.Unspecified)
            {
                return await ReadNodesAsync(nodeIds, optionalAttributes, ct).ConfigureAwait(false);
            }

            var nodeCollection = new NodeCollection(nodeIds.Count);

            // determine attributes to read for nodeclass
            var attributesPerNodeId = new List<IDictionary<uint, DataValue>>(nodeIds.Count);
            var attributesToRead = new ReadValueIdCollection();

            CreateNodeClassAttributesReadNodesRequest(
                nodeIds, nodeClass,
                attributesToRead, attributesPerNodeId,
                nodeCollection,
                optionalAttributes);

            var readResponse = await ReadAsync(
                null,
                0,
                TimestampsToReturn.Neither,
                attributesToRead,
                ct).ConfigureAwait(false);

            var values = readResponse.Results;
            var diagnosticInfos = readResponse.DiagnosticInfos;

            ClientBase.ValidateResponse(values, attributesToRead);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, attributesToRead);

            var serviceResults = new ServiceResult[nodeIds.Count].ToList();
            ProcessAttributesReadNodesResponse(
                readResponse.ResponseHeader,
                attributesToRead, attributesPerNodeId,
                values, diagnosticInfos,
                nodeCollection, serviceResults);

            return (nodeCollection, serviceResults);
        }

        /// <inheritdoc/>
        public async Task<(IList<Node>, IList<ServiceResult>)> ReadNodesAsync(
            IList<NodeId> nodeIds,
            bool optionalAttributes = false,
            CancellationToken ct = default)
        {
            if (nodeIds.Count == 0)
            {
                return (new List<Node>(), new List<ServiceResult>());
            }

            var nodeCollection = new NodeCollection(nodeIds.Count);
            var itemsToRead = new ReadValueIdCollection(nodeIds.Count);

            // first read only nodeclasses for nodes from server.
            itemsToRead = new ReadValueIdCollection(
                nodeIds.Select(nodeId =>
                    new ReadValueId
                    {
                        NodeId = nodeId,
                        AttributeId = Attributes.NodeClass
                    }));

            var readResponse = await ReadAsync(
                null,
                0,
                TimestampsToReturn.Neither,
                itemsToRead,
                ct).ConfigureAwait(false);

            var nodeClassValues = readResponse.Results;
            var diagnosticInfos = readResponse.DiagnosticInfos;

            ClientBase.ValidateResponse(nodeClassValues, itemsToRead);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, itemsToRead);

            // second determine attributes to read per nodeclass
            var attributesPerNodeId = new List<IDictionary<uint, DataValue>>(nodeIds.Count);
            var serviceResults = new List<ServiceResult>(nodeIds.Count);
            var attributesToRead = new ReadValueIdCollection();

            CreateAttributesReadNodesRequest(
                readResponse.ResponseHeader,
                itemsToRead, nodeClassValues, diagnosticInfos,
                attributesToRead, attributesPerNodeId, nodeCollection, serviceResults,
                optionalAttributes);

            if (attributesToRead.Count > 0)
            {
                readResponse = await ReadAsync(
                    null,
                    0,
                    TimestampsToReturn.Neither,
                    attributesToRead, ct).ConfigureAwait(false);

                var values = readResponse.Results;
                diagnosticInfos = readResponse.DiagnosticInfos;

                ClientBase.ValidateResponse(values, attributesToRead);
                ClientBase.ValidateDiagnosticInfos(diagnosticInfos, attributesToRead);

                ProcessAttributesReadNodesResponse(
                    readResponse.ResponseHeader,
                    attributesToRead, attributesPerNodeId,
                    values, diagnosticInfos,
                    nodeCollection, serviceResults);
            }

            return (nodeCollection, serviceResults);
        }

        /// <inheritdoc/>
        public Task<Node> ReadNodeAsync(
            NodeId nodeId,
            CancellationToken ct = default)
        {
            return ReadNodeAsync(nodeId, NodeClass.Unspecified, true, ct);
        }

        /// <inheritdoc/>
        public async Task<Node> ReadNodeAsync(
            NodeId nodeId,
            NodeClass nodeClass,
            bool optionalAttributes = true,
            CancellationToken ct = default)
        {
            // build list of attributes.
            IDictionary<uint, DataValue> attributes = CreateAttributes(nodeClass, optionalAttributes);

            // build list of values to read.
            var itemsToRead = new ReadValueIdCollection();
            foreach (var attributeId in attributes.Keys)
            {
                var itemToRead = new ReadValueId
                {
                    NodeId = nodeId,
                    AttributeId = attributeId
                };
                itemsToRead.Add(itemToRead);
            }

            // read from server.
            var readResponse = await ReadAsync(
                null,
                0,
                TimestampsToReturn.Neither,
                itemsToRead, ct).ConfigureAwait(false);

            var values = readResponse.Results;
            var diagnosticInfos = readResponse.DiagnosticInfos;

            ClientBase.ValidateResponse(values, itemsToRead);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, itemsToRead);

            return ProcessReadResponse(readResponse.ResponseHeader, attributes, itemsToRead, values, diagnosticInfos);
        }

        /// <inheritdoc/>
        public async Task<DataValue> ReadValueAsync(
            NodeId nodeId,
            CancellationToken ct = default)
        {
            var itemToRead = new ReadValueId
            {
                NodeId = nodeId,
                AttributeId = Attributes.Value
            };

            var itemsToRead = new ReadValueIdCollection {
                itemToRead
            };

            // read from server.
            var readResponse = await ReadAsync(
                null,
                0,
                TimestampsToReturn.Both,
                itemsToRead,
                ct).ConfigureAwait(false);

            var values = readResponse.Results;
            var diagnosticInfos = readResponse.DiagnosticInfos;

            ClientBase.ValidateResponse(values, itemsToRead);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, itemsToRead);

            if (StatusCode.IsBad(values[0].StatusCode))
            {
                var result = ClientBase.GetResult(values[0].StatusCode, 0, diagnosticInfos, readResponse.ResponseHeader);
                throw new ServiceResultException(result);
            }

            return values[0];
        }

        /// <inheritdoc/>
        public async Task<(DataValueCollection, IList<ServiceResult>)> ReadValuesAsync(
            IList<NodeId> nodeIds,
            CancellationToken ct = default)
        {
            if (nodeIds.Count == 0)
            {
                return (new DataValueCollection(), new List<ServiceResult>());
            }

            // read all values from server.
            var itemsToRead = new ReadValueIdCollection(
                nodeIds.Select(nodeId =>
                    new ReadValueId
                    {
                        NodeId = nodeId,
                        AttributeId = Attributes.Value
                    }));

            // read from server.
            var errors = new List<ServiceResult>(itemsToRead.Count);

            var readResponse = await ReadAsync(
                null,
                0,
                TimestampsToReturn.Both,
                itemsToRead, ct).ConfigureAwait(false);

            var values = readResponse.Results;
            var diagnosticInfos = readResponse.DiagnosticInfos;

            ClientBase.ValidateResponse(values, itemsToRead);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, itemsToRead);

            foreach (var value in values)
            {
                var result = ServiceResult.Good;
                if (StatusCode.IsBad(value.StatusCode))
                {
                    result = ClientBase.GetResult(values[0].StatusCode, 0, diagnosticInfos, readResponse.ResponseHeader);
                }
                errors.Add(result);
            }

            return (values, errors);
        }

        /// <inheritdoc/>
        public async Task<(
            ResponseHeader responseHeader,
            ByteStringCollection continuationPoints,
            IList<ReferenceDescriptionCollection> referencesList,
            IList<ServiceResult> errors
            )> BrowseAsync(
            RequestHeader requestHeader,
            ViewDescription view,
            IList<NodeId> nodesToBrowse,
            uint maxResultsToReturn,
            BrowseDirection browseDirection,
            NodeId referenceTypeId,
            bool includeSubtypes,
            uint nodeClassMask,
            CancellationToken ct = default)
        {
            var browseDescriptions = new BrowseDescriptionCollection();
            foreach (var nodeToBrowse in nodesToBrowse)
            {
                var description = new BrowseDescription
                {
                    NodeId = nodeToBrowse,
                    BrowseDirection = browseDirection,
                    ReferenceTypeId = referenceTypeId,
                    IncludeSubtypes = includeSubtypes,
                    NodeClassMask = nodeClassMask,
                    ResultMask = (uint)BrowseResultMask.All
                };

                browseDescriptions.Add(description);
            }

            var browseResponse = await BrowseAsync(
                requestHeader,
                view,
                maxResultsToReturn,
                browseDescriptions,
                ct).ConfigureAwait(false);

            ClientBase.ValidateResponse(browseResponse.ResponseHeader);
            var results = browseResponse.Results;
            var diagnosticInfos = browseResponse.DiagnosticInfos;

            ClientBase.ValidateResponse(results, browseDescriptions);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, browseDescriptions);

            var ii = 0;
            var errors = new List<ServiceResult>();
            var continuationPoints = new ByteStringCollection();
            var referencesList = new List<ReferenceDescriptionCollection>();
            foreach (var result in results)
            {
                if (StatusCode.IsBad(result.StatusCode))
                {
                    errors.Add(new ServiceResult(result.StatusCode, ii, diagnosticInfos, browseResponse.ResponseHeader.StringTable));
                }
                else
                {
                    errors.Add(ServiceResult.Good);
                }
                continuationPoints.Add(result.ContinuationPoint);
                referencesList.Add(result.References);
                ii++;
            }

            return (browseResponse.ResponseHeader, continuationPoints, referencesList, errors);
        }


        /// <inheritdoc/>
        public async Task<(
            ResponseHeader responseHeader,
            ByteStringCollection revisedContinuationPoints,
            IList<ReferenceDescriptionCollection> referencesList,
            IList<ServiceResult> errors
            )> BrowseNextAsync(
            RequestHeader requestHeader,
            ByteStringCollection continuationPoints,
            bool releaseContinuationPoint,
            CancellationToken ct = default)
        {
            var response = await base.BrowseNextAsync(
                requestHeader,
                releaseContinuationPoint,
                continuationPoints,
                ct).ConfigureAwait(false);

            ClientBase.ValidateResponse(response.ResponseHeader);

            var results = response.Results;
            var diagnosticInfos = response.DiagnosticInfos;

            ClientBase.ValidateResponse(results, continuationPoints);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, continuationPoints);

            var ii = 0;
            var errors = new List<ServiceResult>();
            var revisedContinuationPoints = new ByteStringCollection();
            var referencesList = new List<ReferenceDescriptionCollection>();
            foreach (var result in results)
            {
                if (StatusCode.IsBad(result.StatusCode))
                {
                    errors.Add(new ServiceResult(result.StatusCode, ii, diagnosticInfos, response.ResponseHeader.StringTable));
                }
                else
                {
                    errors.Add(ServiceResult.Good);
                }
                revisedContinuationPoints.Add(result.ContinuationPoint);
                referencesList.Add(result.References);
                ii++;
            }

            return (response.ResponseHeader, revisedContinuationPoints, referencesList, errors);
        }

        /// <inheritdoc/>
        public async Task<(
            IList<ReferenceDescriptionCollection>,
            IList<ServiceResult>
            )>
                ManagedBrowseAsync(
                    RequestHeader requestHeader,
                    ViewDescription view,
                    IList<NodeId> nodesToBrowse,
                    uint maxResultsToReturn,
                    BrowseDirection browseDirection,
                    NodeId referenceTypeId,
                    bool includeSubtypes,
                    uint nodeClassMask,
                    CancellationToken ct = default
            )
        {
            var count = nodesToBrowse.Count;
            var result = new List<ReferenceDescriptionCollection>(count);
            var errors = new List<ServiceResult>(count);

            // first attempt for implementation: create the references for the output in advance.
            // optimize later, when everything works fine.
            for (var i = 0; i < nodesToBrowse.Count; i++)
            {
                result.Add(new ReferenceDescriptionCollection());
                errors.Add(new ServiceResult(StatusCodes.Good));
            }

            try
            {
                // in the first pass, we browse all nodes from the input.
                // Some nodes may need to be browsed again, these are then fed into the next pass.
                var nodesToBrowseForPass = new List<NodeId>(count);
                nodesToBrowseForPass.AddRange(nodesToBrowse);

                var resultForPass = new List<ReferenceDescriptionCollection>(count);
                resultForPass.AddRange(result);

                var errorsForPass = new List<ServiceResult>(count);
                errorsForPass.AddRange(errors);

                var passCount = 0;

                do
                {
                    var badNoCPErrorsPerPass = 0;
                    var badCPInvalidErrorsPerPass = 0;
                    var otherErrorsPerPass = 0;
                    var maxNodesPerBrowse = OperationLimits.MaxNodesPerBrowse;

                    if (ContinuationPointPolicy == ContinuationPointPolicy.Balanced && ServerMaxContinuationPointsPerBrowse > 0)
                    {
                        maxNodesPerBrowse = ServerMaxContinuationPointsPerBrowse < maxNodesPerBrowse ? ServerMaxContinuationPointsPerBrowse : maxNodesPerBrowse;
                    }

                    // split input into batches
                    var batchOffset = 0;

                    var nodesToBrowseForNextPass = new List<NodeId>();
                    var referenceDescriptionsForNextPass = new List<ReferenceDescriptionCollection>();
                    var errorsForNextPass = new List<ServiceResult>();

                    // loop over the batches
                    foreach (var nodesToBrowseBatch in ((List<NodeId>)nodesToBrowseForPass).Batch<NodeId, List<NodeId>>(maxNodesPerBrowse))
                    {
                        var nodesToBrowseBatchCount = nodesToBrowseBatch.Count;

                        (
                            var resultForBatch,
                            var errorsForBatch
                        )
                        =
                        await BrowseWithBrowseNextAsync(requestHeader, view, nodesToBrowseBatch, maxResultsToReturn, browseDirection, referenceTypeId, includeSubtypes, nodeClassMask
, ct).ConfigureAwait(false);

                        var resultOffset = batchOffset;
                        for (var ii = 0; ii < nodesToBrowseBatchCount; ii++)
                        {
                            var statusCode = errorsForBatch[ii].StatusCode;
                            if (StatusCode.IsBad(statusCode))
                            {
                                var addToNextPass = false;
                                if (statusCode == StatusCodes.BadNoContinuationPoints)
                                {
                                    addToNextPass = true;
                                    badNoCPErrorsPerPass++;
                                }
                                else if (statusCode == StatusCodes.BadContinuationPointInvalid)
                                {
                                    addToNextPass = true;
                                    badCPInvalidErrorsPerPass++;
                                }
                                else
                                {
                                    otherErrorsPerPass++;
                                }

                                if (addToNextPass)
                                {
                                    nodesToBrowseForNextPass.Add(nodesToBrowseForPass[resultOffset]);
                                    referenceDescriptionsForNextPass.Add(resultForPass[resultOffset]);
                                    errorsForNextPass.Add(errorsForPass[resultOffset]);
                                }
                            }

                            resultForPass[resultOffset].Clear();
                            resultForPass[resultOffset].AddRange(resultForBatch[ii]);
                            errorsForPass[resultOffset] = errorsForBatch[ii];
                            resultOffset++;
                        }

                        batchOffset += nodesToBrowseBatchCount;
                    }

                    resultForPass = referenceDescriptionsForNextPass;
                    referenceDescriptionsForNextPass = new List<ReferenceDescriptionCollection>();

                    errorsForPass = errorsForNextPass;
                    errorsForNextPass = new List<ServiceResult>();

                    nodesToBrowseForPass = nodesToBrowseForNextPass;
                    nodesToBrowseForNextPass = new List<NodeId>();

                    var aggregatedErrorMessage = "ManagedBrowse: in pass {0}, {1} {2} occured with a status code {3}.";

                    if (badCPInvalidErrorsPerPass > 0)
                    {
                        Utils.LogDebug(aggregatedErrorMessage, passCount, badCPInvalidErrorsPerPass,
                            badCPInvalidErrorsPerPass == 1 ? "error" : "errors", nameof(StatusCodes.BadContinuationPointInvalid));
                    }
                    if (badNoCPErrorsPerPass > 0)
                    {
                        Utils.LogDebug(aggregatedErrorMessage, passCount, badNoCPErrorsPerPass,
                            badNoCPErrorsPerPass == 1 ? "error" : "errors", nameof(StatusCodes.BadNoContinuationPoints));
                    }
                    if (otherErrorsPerPass > 0)
                    {
                        Utils.LogDebug(aggregatedErrorMessage, passCount, otherErrorsPerPass,
                            otherErrorsPerPass == 1 ? "error" : "errors", $"different from {nameof(StatusCodes.BadNoContinuationPoints)} or {nameof(StatusCodes.BadContinuationPointInvalid)}");
                    }
                    if (otherErrorsPerPass == 0 && badCPInvalidErrorsPerPass == 0 && badNoCPErrorsPerPass == 0)
                    {
                        Utils.LogTrace("ManagedBrowse completed with no errors.");
                    }

                    passCount++;
                } while (nodesToBrowseForPass.Count > 0);
            }
            catch (Exception ex)
            {
                Utils.LogError(ex, "ManagedBrowse failed");
            }

            return (result, errors);
        }

        /// <summary>
        /// Used to pass on references to the Service results in the loop in ManagedBrowseAsync.
        /// </summary>
        private class ReferenceWrapper<T>
        {
            public T reference { get; set; }
        }

        /// <summary>
        /// Call the browse service asynchronously and call browse next,
        /// if applicable, immediately afterwards. Observe proper treatment
        /// of specific service results, specifically
        /// BadNoContinuationPoint and BadContinuationPointInvalid
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="view"></param>
        /// <param name="nodeIds"></param>
        /// <param name="maxResultsToReturn"></param>
        /// <param name="browseDirection"></param>
        /// <param name="referenceTypeId"></param>
        /// <param name="includeSubtypes"></param>
        /// <param name="nodeClassMask"></param>
        /// <param name="ct"></param>
        private async Task<(
            IList<ReferenceDescriptionCollection>,
            IList<ServiceResult>
            )>
            BrowseWithBrowseNextAsync(
            RequestHeader requestHeader,
            ViewDescription view,
            List<NodeId> nodeIds,
            uint maxResultsToReturn,
            BrowseDirection browseDirection,
            NodeId referenceTypeId,
            bool includeSubtypes,
            uint nodeClassMask,
            CancellationToken ct = default
            )
        {
            if (requestHeader != null)
            {
                requestHeader.RequestHandle = 0;
            }

            var result = new List<ReferenceDescriptionCollection>(nodeIds.Count);

            (
                _,
                var continuationPoints,
                var referenceDescriptions,
                var errors
            ) =
            await BrowseAsync(
                requestHeader,
                view,
                nodeIds,
                maxResultsToReturn,
                browseDirection,
                referenceTypeId,
                includeSubtypes,
                nodeClassMask,
                ct).ConfigureAwait(false);

            result.AddRange(referenceDescriptions);

            // process any continuation point.
            var previousResults = result;
            var errorAnchors = new List<ReferenceWrapper<ServiceResult>>();
            var previousErrors = new List<ReferenceWrapper<ServiceResult>>();
            foreach (var error in errors)
            {
                previousErrors.Add(new ReferenceWrapper<ServiceResult> { reference = error });
                errorAnchors.Add(previousErrors[^1]);
            }

            var nextContinuationPoints = new ByteStringCollection();
            var nextResults = new List<ReferenceDescriptionCollection>();
            var nextErrors = new List<ReferenceWrapper<ServiceResult>>();

            for (var ii = 0; ii < nodeIds.Count; ii++)
            {
                if (continuationPoints[ii] != null && !StatusCode.IsBad(previousErrors[ii].reference.StatusCode))
                {
                    nextContinuationPoints.Add(continuationPoints[ii]);
                    nextResults.Add(previousResults[ii]);
                    nextErrors.Add(previousErrors[ii]);
                }
            }
            while (nextContinuationPoints.Count > 0)
            {
                if (requestHeader != null)
                {
                    requestHeader.RequestHandle = 0;
                }

                (
                    _,
                    var revisedContinuationPoints,
                    var browseNextResults,
                    var browseNextErrors
                ) = await BrowseNextAsync(
                    requestHeader,
                    nextContinuationPoints,
                    false,
                    ct
                    ).ConfigureAwait(false);

                for (var ii = 0; ii < browseNextResults.Count; ii++)
                {
                    nextResults[ii].AddRange(browseNextResults[ii]);
                    nextErrors[ii].reference = browseNextErrors[ii];
                }

                previousResults = nextResults;
                previousErrors = nextErrors;

                nextResults = new List<ReferenceDescriptionCollection>();
                nextErrors = new List<ReferenceWrapper<ServiceResult>>();
                nextContinuationPoints = new ByteStringCollection();

                for (var ii = 0; ii < revisedContinuationPoints.Count; ii++)
                {
                    if (revisedContinuationPoints[ii] != null && !StatusCode.IsBad(browseNextErrors[ii].StatusCode))
                    {
                        nextContinuationPoints.Add(revisedContinuationPoints[ii]);
                        nextResults.Add(previousResults[ii]);
                        nextErrors.Add(previousErrors[ii]);
                    }
                }
            }
            var finalErrors = new List<ServiceResult>(errorAnchors.Count);
            foreach (var errorReference in errorAnchors)
            {
                finalErrors.Add(errorReference.reference);
            }

            return (result, finalErrors);
        }


        /// <inheritdoc/>
        public async Task<IList<object>> CallAsync(NodeId objectId, NodeId methodId, CancellationToken ct = default, params object[] args)
        {
            var inputArguments = new VariantCollection();

            if (args != null)
            {
                for (var ii = 0; ii < args.Length; ii++)
                {
                    inputArguments.Add(new Variant(args[ii]));
                }
            }

            var request = new CallMethodRequest();

            request.ObjectId = objectId;
            request.MethodId = methodId;
            request.InputArguments = inputArguments;

            var requests = new CallMethodRequestCollection();
            requests.Add(request);

            CallMethodResultCollection results;
            DiagnosticInfoCollection diagnosticInfos;

            var response = await base.CallAsync(null, requests, ct).ConfigureAwait(false);

            results = response.Results;
            diagnosticInfos = response.DiagnosticInfos;

            ClientBase.ValidateResponse(results, requests);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, requests);

            if (StatusCode.IsBad(results[0].StatusCode))
            {
                throw ServiceResultException.Create(results[0].StatusCode, 0, diagnosticInfos, response.ResponseHeader.StringTable);
            }

            var outputArguments = new List<object>();

            foreach (var arg in results[0].OutputArguments)
            {
                outputArguments.Add(arg.Value);
            }

            return outputArguments;
        }

        /// <inheritdoc/>
        public async Task<ReferenceDescriptionCollection> FetchReferencesAsync(
            NodeId nodeId,
            CancellationToken ct = default)
        {
            (
                var descriptions,
                _
            ) =
                await ManagedBrowseAsync(
                    null,
                    null,
                    new NodeId[] { nodeId },
                    0,
                    BrowseDirection.Both,
                    null,
                    true,
                    0,
                    ct).ConfigureAwait(false);
            return descriptions[0];
        }

        /// <inheritdoc/>
        public Task<(IList<ReferenceDescriptionCollection>, IList<ServiceResult>)> FetchReferencesAsync(
            IList<NodeId> nodeIds,
            CancellationToken ct = default)
            => ManagedBrowseAsync(
                null,
                null,
                nodeIds,
                0,
                BrowseDirection.Both,
                null,
                true,
                0,
                ct
                );

        /// <summary>
        /// Recreates a session based on a specified template.
        /// </summary>
        /// <param name="sessionTemplate">The Session object to use as template</param>
        /// <param name="ct"></param>
        /// <returns>The new session object.</returns>
        public static async Task<Session> RecreateAsync(Session sessionTemplate, CancellationToken ct = default)
        {
            var messageContext = sessionTemplate.m_configuration.CreateMessageContext();
            messageContext.Factory = sessionTemplate.Factory;

            // create the channel object used to connect to the server.
#pragma warning disable CA2000 // Dispose objects before losing scope
            var channel = SessionChannel.Create(
                sessionTemplate.m_configuration,
                sessionTemplate.ConfiguredEndpoint.Description,
                sessionTemplate.ConfiguredEndpoint.Configuration,
                sessionTemplate.m_instanceCertificate,
                sessionTemplate.m_configuration.SecurityConfiguration.SendCertificateChain ?
                    sessionTemplate.m_instanceCertificateChain : null,
                messageContext);
#pragma warning restore CA2000 // Dispose objects before losing scope

            // create the session object.
            var session = sessionTemplate.CloneSession(channel, true);

            try
            {
                // open the session.
                await session.OpenAsync(
                    sessionTemplate.SessionName,
                    (uint)sessionTemplate.SessionTimeout,
                    sessionTemplate.Identity,
                    sessionTemplate.PreferredLocales,
                    sessionTemplate.m_checkDomain,
                    ct).ConfigureAwait(false);

                await session.RecreateSubscriptionsAsync(sessionTemplate.Subscriptions, ct).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                session.Dispose();
                ThrowCouldNotRecreateSessionException(e, sessionTemplate.m_sessionName);
            }

            return session;
        }

        /// <summary>
        /// Recreates a session based on a specified template.
        /// </summary>
        /// <param name="sessionTemplate">The Session object to use as template</param>
        /// <param name="connection">The waiting reverse connection.</param>
        /// <param name="ct"></param>
        /// <returns>The new session object.</returns>
        public static async Task<Session> RecreateAsync(Session sessionTemplate, ITransportWaitingConnection connection, CancellationToken ct = default)
        {
            var messageContext = sessionTemplate.m_configuration.CreateMessageContext();
            messageContext.Factory = sessionTemplate.Factory;

            // create the channel object used to connect to the server.
#pragma warning disable CA2000 // Dispose objects before losing scope
            var channel = SessionChannel.Create(
                sessionTemplate.m_configuration,
                connection,
                sessionTemplate.m_endpoint.Description,
                sessionTemplate.m_endpoint.Configuration,
                sessionTemplate.m_instanceCertificate,
                sessionTemplate.m_configuration.SecurityConfiguration.SendCertificateChain ?
                    sessionTemplate.m_instanceCertificateChain : null,
                messageContext);
#pragma warning restore CA2000 // Dispose objects before losing scope

            // create the session object.
            var session = sessionTemplate.CloneSession(channel, true);

            try
            {
                // open the session.
                await session.OpenAsync(
                    sessionTemplate.m_sessionName,
                    (uint)sessionTemplate.m_sessionTimeout,
                    sessionTemplate.m_identity,
                    sessionTemplate.m_preferredLocales,
                    sessionTemplate.m_checkDomain,
                    ct).ConfigureAwait(false);

                await session.RecreateSubscriptionsAsync(sessionTemplate.Subscriptions, ct).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                session.Dispose();
                ThrowCouldNotRecreateSessionException(e, sessionTemplate.m_sessionName);
            }

            return session;
        }

        /// <summary>
        /// Recreates a session based on a specified template using the provided channel.
        /// </summary>
        /// <param name="sessionTemplate">The Session object to use as template</param>
        /// <param name="transportChannel">The waiting reverse connection.</param>
        /// <param name="ct"></param>
        /// <returns>The new session object.</returns>
        public static async Task<Session> RecreateAsync(Session sessionTemplate, ITransportChannel transportChannel, CancellationToken ct = default)
        {
            if (transportChannel == null)
            {
                return await Session.RecreateAsync(sessionTemplate, ct).ConfigureAwait(false);
            }

            var messageContext = sessionTemplate.m_configuration.CreateMessageContext();
            messageContext.Factory = sessionTemplate.Factory;

            // create the session object.
            var session = sessionTemplate.CloneSession(transportChannel, true);

            try
            {
                // open the session.
                await session.OpenAsync(
                    sessionTemplate.m_sessionName,
                    (uint)sessionTemplate.m_sessionTimeout,
                    sessionTemplate.m_identity,
                    sessionTemplate.m_preferredLocales,
                    sessionTemplate.m_checkDomain,
                    ct).ConfigureAwait(false);

                // create the subscriptions.
                foreach (var subscription in session.Subscriptions)
                {
                    await subscription.CreateAsync(ct).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                session.Dispose();
                ThrowCouldNotRecreateSessionException(e, sessionTemplate.m_sessionName);
            }

            return session;
        }

        /// <inheritdoc/>
        public override Task<StatusCode> CloseAsync(CancellationToken ct = default)
        {
            return CloseAsync(m_keepAliveInterval, true, ct);
        }

        /// <inheritdoc/>
        public Task<StatusCode> CloseAsync(bool closeChannel, CancellationToken ct = default)
        {
            return CloseAsync(m_keepAliveInterval, closeChannel, ct);
        }

        /// <inheritdoc/>
        public Task<StatusCode> CloseAsync(int timeout, CancellationToken ct = default)
            => CloseAsync(timeout, true, ct);

        /// <inheritdoc/>
        public virtual async Task<StatusCode> CloseAsync(int timeout, bool closeChannel, CancellationToken ct = default)
        {
            // check if already called.
            if (Disposed)
            {
                return StatusCodes.Good;
            }

            StatusCode result = StatusCodes.Good;

            // stop the keep alive timer.
            StopKeepAliveTimer();

            // check if correctly connected.
            var connected = Connected;

            // halt all background threads.
            if (connected && m_SessionClosing != null)
            {
                try
                {
                    m_SessionClosing(this, null);
                }
                catch (Exception e)
                {
                    Utils.LogError(e, "Session: Unexpected error raising SessionClosing event.");
                }
            }

            // close the session with the server.
            if (connected && !KeepAliveStopped)
            {
                try
                {
                    // close the session and delete all subscriptions if specified.
                    var requestHeader = new RequestHeader()
                    {
                        TimeoutHint = timeout > 0 ? (uint)timeout : (uint)(this.OperationTimeout > 0 ? this.OperationTimeout : 0)
                    };
                    var response = await base.CloseSessionAsync(requestHeader, m_deleteSubscriptionsOnClose, ct).ConfigureAwait(false);

                    if (closeChannel)
                    {
                        await CloseChannelAsync(ct).ConfigureAwait(false);
                    }

                    // raised notification indicating the session is closed.
                    SessionCreated(null, null);
                }
                // don't throw errors on disconnect, but return them
                // so the caller can log the error.
                catch (ServiceResultException sre)
                {
                    result = sre.StatusCode;
                }
                catch (Exception)
                {
                    result = StatusCodes.Bad;
                }
            }

            // clean up.
            if (closeChannel)
            {
                Dispose();
            }

            return result;
        }

        /// <inheritdoc/>
        public Task ReconnectAsync(CancellationToken ct)
            => ReconnectAsync(null, null, ct);

        /// <inheritdoc/>
        public Task ReconnectAsync(ITransportWaitingConnection connection, CancellationToken ct)
            => ReconnectAsync(connection, null, ct);

        /// <inheritdoc/>
        public Task ReconnectAsync(ITransportChannel channel, CancellationToken ct)
            => ReconnectAsync(null, channel, ct);

        /// <summary>
        /// Reconnects to the server after a network failure using a waiting connection.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transportChannel"></param>
        /// <param name="ct"></param>
        /// <exception cref="ServiceResultException"></exception>
        private async Task ReconnectAsync(ITransportWaitingConnection connection, ITransportChannel transportChannel, CancellationToken ct)
        {
            var resetReconnect = false;
            await m_reconnectLock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var reconnecting = m_reconnecting;
                m_reconnecting = true;
                resetReconnect = true;
                m_reconnectLock.Release();

                // check if already connecting.
                if (reconnecting)
                {
                    Utils.LogWarning("Session is already attempting to reconnect.");

                    throw ServiceResultException.Create(
                        StatusCodes.BadInvalidState,
                        "Session is already attempting to reconnect.");
                }

                StopKeepAliveTimer();

                var result = PrepareReconnectBeginActivate(
                    connection,
                    transportChannel);

                const string timeoutMessage = "ACTIVATE SESSION ASYNC timed out. {0}/{1}";
                if (result is ChannelAsyncOperation<int> operation)
                {
                    try
                    {
                        _ = await operation.EndAsync(kReconnectTimeout / 2, true, ct).ConfigureAwait(false);
                    }
                    catch (ServiceResultException sre)
                    {
                        if (sre.StatusCode == StatusCodes.BadRequestInterrupted)
                        {
                            var error = ServiceResult.Create(StatusCodes.BadRequestTimeout, timeoutMessage,
                                GoodPublishRequestCount, OutstandingRequestCount);
                            Utils.LogWarning("WARNING: {0}", error.ToString());
                            operation.Fault(false, error);
                        }
                    }
                }
                else if (!result.AsyncWaitHandle.WaitOne(kReconnectTimeout / 2))
                {
                    Utils.LogWarning(timeoutMessage, GoodPublishRequestCount, OutstandingRequestCount);
                }

                // reactivate session.
                byte[] serverNonce = null;
                StatusCodeCollection certificateResults = null;
                DiagnosticInfoCollection certificateDiagnosticInfos = null;

                EndActivateSession(
                    result,
                    out serverNonce,
                    out certificateResults,
                    out certificateDiagnosticInfos);

                Utils.LogInfo("Session RECONNECT {0} completed successfully.", SessionId);

                lock (SyncRoot)
                {
                    m_previousServerNonce = m_serverNonce;
                    m_serverNonce = serverNonce;
                }

                await m_reconnectLock.WaitAsync(ct).ConfigureAwait(false);
                m_reconnecting = false;
                resetReconnect = false;
                m_reconnectLock.Release();

                StartPublishing(OperationTimeout, true);

                StartKeepAliveTimer();

                IndicateSessionConfigurationChanged();
            }
            finally
            {
                if (resetReconnect)
                {
                    await m_reconnectLock.WaitAsync(ct).ConfigureAwait(false);
                    m_reconnecting = false;
                    m_reconnectLock.Release();
                }
            }
        }

        /// <inheritdoc/>
        public async Task<(bool, ServiceResult)> RepublishAsync(uint subscriptionId, uint sequenceNumber, CancellationToken ct)
        {
            // send republish request.
            var requestHeader = new RequestHeader
            {
                TimeoutHint = (uint)OperationTimeout,
                ReturnDiagnostics = (uint)(int)ReturnDiagnostics,
                RequestHandle = Utils.IncrementIdentifier(ref m_publishCounter)
            };

            try
            {
                Utils.LogInfo("Requesting RepublishAsync for {0}-{1}", subscriptionId, sequenceNumber);

                // request republish.
                var response = await RepublishAsync(
                    requestHeader,
                    subscriptionId,
                    sequenceNumber,
                    ct).ConfigureAwait(false);
                var responseHeader = response.ResponseHeader;
                var notificationMessage = response.NotificationMessage;

                Utils.LogInfo("Received RepublishAsync for {0}-{1}-{2}", subscriptionId, sequenceNumber, responseHeader.ServiceResult);

                // process response.
                ProcessPublishResponse(
                    responseHeader,
                    subscriptionId,
                    null,
                    false,
                    notificationMessage);

                return (true, ServiceResult.Good);
            }
            catch (Exception e)
            {
                return ProcessRepublishResponseError(e, subscriptionId, sequenceNumber);
            }
        }

        /// <summary>
        /// Recreate the subscriptions in a reconnected session.
        /// Uses Transfer service if <see cref="TransferSubscriptionsOnReconnect"/> is set to <c>true</c>.
        /// </summary>
        /// <param name="subscriptionsTemplate">The template for the subscriptions.</param>
        /// <param name="ct"></param>
        private async Task RecreateSubscriptionsAsync(IEnumerable<Subscription> subscriptionsTemplate, CancellationToken ct)
        {
            var transferred = false;
            if (TransferSubscriptionsOnReconnect)
            {
                try
                {
                    transferred = await TransferSubscriptionsAsync(new SubscriptionCollection(subscriptionsTemplate), false, ct).ConfigureAwait(false);
                }
                catch (ServiceResultException sre)
                {
                    if (sre.StatusCode == StatusCodes.BadServiceUnsupported)
                    {
                        TransferSubscriptionsOnReconnect = false;
                        Utils.LogWarning("Transfer subscription unsupported, TransferSubscriptionsOnReconnect set to false.");
                    }
                    else
                    {
                        Utils.LogError(sre, "Transfer subscriptions failed.");
                    }
                }
                catch (Exception ex)
                {
                    Utils.LogError(ex, "Unexpected Transfer subscriptions error.");
                }
            }

            if (!transferred)
            {
                // Create the subscriptions which were not transferred.
                foreach (var subscription in Subscriptions)
                {
                    if (!subscription.Created)
                    {
                        await subscription.CreateAsync(ct).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
