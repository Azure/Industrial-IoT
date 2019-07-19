// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

// #define USE_TASK_RUN

namespace Opc.Ua.Client {
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Session Client async extensions
    /// </summary>
    public static class SessionClientEx {

        /// <summary>
        /// Async create session service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="requestHeader"></param>
        /// <param name="clientDescription"></param>
        /// <param name="serverUri"></param>
        /// <param name="endpointUrl"></param>
        /// <param name="sessionName"></param>
        /// <param name="clientNonce"></param>
        /// <param name="clientCertificate"></param>
        /// <param name="requestedSessionTimeout"></param>
        /// <param name="maxResponseMessageSize"></param>
        /// <returns></returns>
        public static Task<CreateSessionResponse> CreateSessionAsync(
            this SessionClient client, RequestHeader requestHeader,
            ApplicationDescription clientDescription, string serverUri,
            string endpointUrl, string sessionName, byte[] clientNonce,
            byte[] clientCertificate, double requestedSessionTimeout,
            uint maxResponseMessageSize) {
#if !USE_TASK_RUN
            return Task.Factory.FromAsync(
                (callback, state) => client.BeginCreateSession(requestHeader,
                    clientDescription, serverUri, endpointUrl, sessionName, clientNonce,
                    clientCertificate, requestedSessionTimeout, maxResponseMessageSize,
                    callback, state),
                result => {
                    var response = client.EndCreateSession(result,
                    out var sessionId, out var authenticationToken,
                    out var revisedSessionTimeout, out var serverNonce,
                    out var serverCertificate, out var serverEndpoints,
                    out var serverSoftwareCertificates, out var serverSignature,
                    out var maxRequestMessageSize);
                    return NewCreateSessionResponse(response, sessionId, authenticationToken,
                        revisedSessionTimeout, serverNonce, serverCertificate, serverEndpoints,
                        serverSoftwareCertificates, serverSignature, maxRequestMessageSize);
                }, TaskCreationOptions.DenyChildAttach);
#else
            return Task.Run(() => {
                var response = client.CreateSession(requestHeader, clientDescription,
                    serverUri, endpointUrl, sessionName, clientNonce, clientCertificate,
                    requestedSessionTimeout, maxResponseMessageSize,
                    out var sessionId, out var authenticationToken,
                    out var revisedSessionTimeout, out var serverNonce,
                    out var serverCertificate, out var serverEndpoints,
                    out var serverSoftwareCertificates, out var serverSignature,
                    out var maxRequestMessageSize);
                return NewCreateSessionResponse(response, sessionId, authenticationToken,
                    revisedSessionTimeout, serverNonce, serverCertificate, serverEndpoints,
                    serverSoftwareCertificates, serverSignature, maxRequestMessageSize);
            });
#endif
        }

        /// <summary>
        /// Async activate session service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="requestHeader"></param>
        /// <param name="clientSignature"></param>
        /// <param name="clientSoftwareCertificates"></param>
        /// <param name="localeIds"></param>
        /// <param name="userIdentityToken"></param>
        /// <param name="userTokenSignature"></param>
        /// <returns></returns>
        public static Task<ActivateSessionResponse> ActivateSessionAsync(
            this SessionClient client, RequestHeader requestHeader, SignatureData clientSignature,
            SignedSoftwareCertificateCollection clientSoftwareCertificates,
            StringCollection localeIds, ExtensionObject userIdentityToken,
            SignatureData userTokenSignature) {
#if !USE_TASK_RUN
            return Task.Factory.FromAsync(
                (callback, state) => client.BeginActivateSession(requestHeader,
                    clientSignature, clientSoftwareCertificates, localeIds, userIdentityToken,
                    userTokenSignature, callback, state),
                result => {
                    var response = client.EndActivateSession(result,
                        out var serverNonce, out var results, out var diagnosticInfos);
                    return NewActivateSessionResponse(response, serverNonce, results,
                        diagnosticInfos);
                }, TaskCreationOptions.DenyChildAttach);
#else
            return Task.Run(() => {
                var response = client.ActivateSession(requestHeader, clientSignature,
                    clientSoftwareCertificates, localeIds, userIdentityToken, userTokenSignature,
                    out var serverNonce, out var results, out var diagnosticInfos);
                return NewActivateSessionResponse(response, serverNonce, results,
                    diagnosticInfos);
            });
#endif
        }

        /// <summary>
        /// Async close session service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="requestHeader"></param>
        /// <param name="deleteSubscriptions"></param>
        /// <returns></returns>
        public static Task<ResponseHeader> CloseSessionAsync(
            this SessionClient client, RequestHeader requestHeader, bool deleteSubscriptions) {
#if !USE_TASK_RUN
            return Task.Factory.FromAsync(
                (callback, state) => client.BeginCloseSession(requestHeader,
                    deleteSubscriptions, callback, state),
                client.EndCloseSession, TaskCreationOptions.DenyChildAttach);
#else
            return Task.Run(() => {
                return client.CloseSession(requestHeader, deleteSubscriptions);
            });
#endif
        }

        /// <summary>
        /// Async browse service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="requestHeader"></param>
        /// <param name="view"></param>
        /// <param name="nodeToBrowse"></param>
        /// <param name="maxResultsToReturn"></param>
        /// <param name="browseDirection"></param>
        /// <param name="referenceTypeId"></param>
        /// <param name="includeSubtypes"></param>
        /// <param name="nodeClassMask"></param>
        /// <param name="resultMask"></param>
        /// <returns></returns>
        public static Task<BrowseResponse> BrowseAsync(this SessionClient client,
            RequestHeader requestHeader, ViewDescription view, NodeId nodeToBrowse,
            uint maxResultsToReturn, BrowseDirection browseDirection,
            NodeId referenceTypeId, bool includeSubtypes, uint nodeClassMask,
            BrowseResultMask resultMask = BrowseResultMask.All) {
            return client.BrowseAsync(requestHeader, view, maxResultsToReturn,
                new BrowseDescriptionCollection {
                    new BrowseDescription {
                        BrowseDirection = browseDirection,
                        IncludeSubtypes = includeSubtypes,
                        NodeClassMask = nodeClassMask,
                        NodeId = nodeToBrowse,
                        ReferenceTypeId = referenceTypeId,
                        ResultMask = (uint)resultMask
                    }
                });
        }

        /// <summary>
        /// Async browse service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="requestHeader"></param>
        /// <param name="view"></param>
        /// <param name="maxResultsToReturn"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public static Task<BrowseResponse> BrowseAsync(this SessionClient client,
            RequestHeader requestHeader, ViewDescription view, uint maxResultsToReturn,
            BrowseDescriptionCollection description) {
#if !USE_TASK_RUN
            return Task.Factory.FromAsync(
                (callback, state) => client.BeginBrowse(requestHeader, view, maxResultsToReturn,
                    description, callback, state),
                result => {
                    var response = client.EndBrowse(result,
                        out var results, out var diagnosticInfos);
                    return NewBrowseResponse(response, results, diagnosticInfos);
                }, TaskCreationOptions.DenyChildAttach);
#else
            return Task.Run(() => {
                var response = client.Browse(requestHeader, view, maxResultsToReturn, description,
                    out var results, out var diagnosticInfos);
                return NewBrowseResponse(response, results, diagnosticInfos);
            });
#endif
        }

        /// <summary>
        /// Async browse next service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="requestHeader"></param>
        /// <param name="releaseContinuationPoints"></param>
        /// <param name="continuationPoints"></param>
        /// <returns></returns>
        public static Task<BrowseResponse> BrowseNextAsync(this SessionClient client,
            RequestHeader requestHeader, bool releaseContinuationPoints,
            ByteStringCollection continuationPoints) {
#if !USE_TASK_RUN
            return Task.Factory.FromAsync(
                (callback, state) => client.BeginBrowseNext(requestHeader, releaseContinuationPoints,
                    continuationPoints, callback, state),
                result => {
                    var response = client.EndBrowseNext(result,
                        out var results, out var diagnosticInfos);
                    return NewBrowseResponse(response, results, diagnosticInfos);
                }, TaskCreationOptions.DenyChildAttach);
#else
            return Task.Run(() => {
                var response = client.BrowseNext(requestHeader, releaseContinuationPoints,
                    continuationPoints, out var results, out var diagnosticInfos);
                return NewBrowseResponse(response, results, diagnosticInfos);
            });
#endif
        }

        /// <summary>
        /// Async translate browse path service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="requestHeader"></param>
        /// <param name="browsePaths"></param>
        /// <returns></returns>
        public static Task<TranslateBrowsePathsToNodeIdsResponse> TranslateBrowsePathsToNodeIdsAsync(
            this SessionClient client, RequestHeader requestHeader, BrowsePathCollection browsePaths) {
#if !USE_TASK_RUN
            return Task.Factory.FromAsync(
                (callback, state) => client.BeginTranslateBrowsePathsToNodeIds(requestHeader,
                    browsePaths, callback, state),
                result => {
                    var response = client.EndTranslateBrowsePathsToNodeIds(result,
                        out var results, out var diagnosticInfos);
                    return NewTranslateBrowsePathsToNodeIdsResponse(response, results, diagnosticInfos);
                }, TaskCreationOptions.DenyChildAttach);
#else
            return Task.Run(() => {
                var response = client.TranslateBrowsePathsToNodeIds(requestHeader, browsePaths,
                    out var results, out var diagnosticInfos);
                return NewTranslateBrowsePathsToNodeIdsResponse(response, results, diagnosticInfos);
            });
#endif
        }

        /// <summary>
        /// Async Read service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="requestHeader"></param>
        /// <param name="maxAge"></param>
        /// <param name="timestampsToReturn"></param>
        /// <param name="nodesToRead"></param>
        /// <returns></returns>
        public static Task<ReadResponse> ReadAsync(this SessionClient client,
            RequestHeader requestHeader, double maxAge, TimestampsToReturn timestampsToReturn,
            ReadValueIdCollection nodesToRead) {
#if !USE_TASK_RUN
            return Task.Factory.FromAsync(
                (callback, state) => client.BeginRead(requestHeader, maxAge, timestampsToReturn,
                    nodesToRead, callback, state),
                result => {
                    var response = client.EndRead(result,
                        out var results, out var diagnosticInfos);
                    return NewReadResponse(response, results, diagnosticInfos);
                }, TaskCreationOptions.DenyChildAttach);
#else
            return Task.Run(() => {
                var response = client.Read(requestHeader, maxAge, timestampsToReturn, nodesToRead,
                    out var results, out var diagnosticInfos);
                return NewReadResponse(response, results, diagnosticInfos);
            });
#endif
        }

        /// <summary>
        /// Async Write service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="requestHeader"></param>
        /// <param name="nodesToWrite"></param>
        /// <returns></returns>
        public static Task<WriteResponse> WriteAsync(this SessionClient client,
            RequestHeader requestHeader, WriteValueCollection nodesToWrite) {
#if !USE_TASK_RUN
            return Task.Factory.FromAsync(
                (callback, state) => client.BeginWrite(requestHeader,
                    nodesToWrite, callback, state),
                result => {
                    var response = client.EndWrite(result,
                        out var results, out var diagnosticInfos);
                    return NewWriteResponse(response, results, diagnosticInfos);
                }, TaskCreationOptions.DenyChildAttach);
#else
            return Task.Run(() => {
                var response = client.Write(requestHeader, nodesToWrite,
                    out var results, out var diagnosticInfos);
                return NewWriteResponse(response, results, diagnosticInfos);
            });
#endif
        }

        /// <summary>
        /// Async Call service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="requestHeader"></param>
        /// <param name="methodsToCall"></param>
        /// <returns></returns>
        public static Task<CallResponse> CallAsync(this SessionClient client,
            RequestHeader requestHeader, CallMethodRequestCollection methodsToCall) {
#if !USE_TASK_RUN
            return Task.Factory.FromAsync(
                (callback, state) => client.BeginCall(requestHeader,
                    methodsToCall, callback, state),
                result => {
                    var response = client.EndCall(result,
                        out var results, out var diagnosticInfos);
                    return NewCallResponse(response, results, diagnosticInfos);
                }, TaskCreationOptions.DenyChildAttach);
#else
            return Task.Run(() => {
                var response = client.Call(requestHeader, methodsToCall,
                    out var results, out var diagnosticInfos);
                return NewCallResponse(response, results, diagnosticInfos);
            });
#endif
        }

        /// <summary>
        /// Async Cancel service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="requestHeader"></param>
        /// <param name="requestHandle"></param>
        /// <returns></returns>
        public static Task<CancelResponse> CancelAsync(this SessionClient client,
            RequestHeader requestHeader, uint requestHandle) {
#if !USE_TASK_RUN
            return Task.Factory.FromAsync(
                (callback, state) => client.BeginCancel(requestHeader,
                    requestHandle, callback, state),
                result => {
                    var response = client.EndCancel(result, out var cancelCount);
                    return NewCancelResponse(response, cancelCount);
                }, TaskCreationOptions.DenyChildAttach);
#else
            return Task.Run(() => {
                var response = client.Cancel(requestHeader, requestHandle,
                    out var cancelCount);
                return NewCancelResponse(response, cancelCount);
            });
#endif
        }

        /// <summary>
        /// History read service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="requestHeader"></param>
        /// <param name="historyReadDetails"></param>
        /// <param name="timestampsToReturn"></param>
        /// <param name="releaseContinuationPoints"></param>
        /// <param name="nodesToRead"></param>
        /// <returns></returns>
        public static Task<HistoryReadResponse> HistoryReadAsync(this SessionClient client,
            RequestHeader requestHeader, ExtensionObject historyReadDetails,
            TimestampsToReturn timestampsToReturn, bool releaseContinuationPoints,
            HistoryReadValueIdCollection nodesToRead) {
#if !USE_TASK_RUN
            return Task.Factory.FromAsync(
                (callback, state) => client.BeginHistoryRead(requestHeader,
                    historyReadDetails, timestampsToReturn, releaseContinuationPoints,
                    nodesToRead, callback, state),
                result => {
                    var response = client.EndHistoryRead(result, out var results,
                        out var diagnosticInfos);
                    return NewHistoryReadResponse(response, results, diagnosticInfos);
                }, TaskCreationOptions.DenyChildAttach);
#else
            return Task.Run(() => {
                var response = client.HistoryRead(requestHeader, historyReadDetails,
                    timestampsToReturn, releaseContinuationPoints, nodesToRead,
                    out var results, out var diagnosticInfos);
                return NewHistoryReadResponse(response, results, diagnosticInfos);
            });
#endif
        }

        /// <summary>
        /// Async History update service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="requestHeader"></param>
        /// <param name="historyUpdateDetails"></param>
        /// <returns></returns>
        public static Task<HistoryUpdateResponse> HistoryUpdateAsync(this SessionClient client,
            RequestHeader requestHeader, ExtensionObjectCollection historyUpdateDetails) {
#if !USE_TASK_RUN
            return Task.Factory.FromAsync(
                (callback, state) => client.BeginHistoryUpdate(requestHeader,
                    historyUpdateDetails, callback, state),
                result => {
                    var response = client.EndHistoryUpdate(result, out var results,
                        out var diagnosticInfos);
                    return NewHistoryUpdateResponse(response, results, diagnosticInfos);
                }, TaskCreationOptions.DenyChildAttach);
#else
            return Task.Run(() => {
                var response = client.HistoryUpdate(requestHeader, historyUpdateDetails,
                    out var results, out var diagnosticInfos);
                return NewHistoryUpdateResponse(response, results, diagnosticInfos);
            });
#endif
        }

        /// <summary>
        /// Async Create monitored item service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="requestHeader"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="timestampsToReturn"></param>
        /// <param name="itemsToCreate"></param>
        /// <returns></returns>
        public static Task<CreateMonitoredItemsResponse> CreateMonitoredItemsAsync(
            this SessionClient client, RequestHeader requestHeader, uint subscriptionId,
            TimestampsToReturn timestampsToReturn,
            MonitoredItemCreateRequestCollection itemsToCreate) {
#if !USE_TASK_RUN
            return Task.Factory.FromAsync(
                (callback, state) => client.BeginCreateMonitoredItems(requestHeader,
                    subscriptionId, timestampsToReturn, itemsToCreate, callback, state),
                result => {
                    var response = client.EndCreateMonitoredItems(result, out var results,
                        out var diagnosticInfos);
                    return NewCreateMonitoredItemsResponse(response, results, diagnosticInfos);
                }, TaskCreationOptions.DenyChildAttach);
#else
            return Task.Run(() => {
                var response = client.CreateMonitoredItems(requestHeader, subscriptionId,
                    timestampsToReturn, itemsToCreate, out var results, out var diagnosticInfos);
                return NewCreateMonitoredItemsResponse(response, results, diagnosticInfos);
            });
#endif
        }

        /// <summary>
        /// Modify monitored items service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="requestHeader"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="timestampsToReturn"></param>
        /// <param name="itemsToModify"></param>
        /// <returns></returns>
        public static Task<ModifyMonitoredItemsResponse> ModifyMonitoredItemsAsync(
            this SessionClient client, RequestHeader requestHeader, uint subscriptionId,
            TimestampsToReturn timestampsToReturn,
            MonitoredItemModifyRequestCollection itemsToModify) {
#if !USE_TASK_RUN
            return Task.Factory.FromAsync(
                (callback, state) => client.BeginModifyMonitoredItems(requestHeader,
                    subscriptionId, timestampsToReturn, itemsToModify, callback, state),
                result => {
                    var response = client.EndModifyMonitoredItems(result, out var results,
                        out var diagnosticInfos);
                    return NewModifyMonitoredItemsResponse(response, results, diagnosticInfos);
                }, TaskCreationOptions.DenyChildAttach);
#else
            return Task.Run(() => {
                var response = client.ModifyMonitoredItems(requestHeader, subscriptionId,
                    timestampsToReturn, itemsToModify, out var results, out var diagnosticInfos);
                return NewModifyMonitoredItemsResponse(response, results, diagnosticInfos);
            });
#endif
        }

        /// <summary>
        /// Async set triggering service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="requestHeader"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="triggeringItemId"></param>
        /// <param name="linksToAdd"></param>
        /// <param name="linksToRemove"></param>
        /// <returns></returns>
        public static Task<SetTriggeringResponse> SetTriggeringAsync(
            this SessionClient client, RequestHeader requestHeader, uint subscriptionId,
            uint triggeringItemId, UInt32Collection linksToAdd,
            UInt32Collection linksToRemove) {
#if !USE_TASK_RUN
            return Task.Factory.FromAsync(
                (callback, state) => client.BeginSetTriggering(requestHeader, subscriptionId,
                    triggeringItemId, linksToAdd, linksToRemove, callback, state),
                result => {
                    var response = client.EndSetTriggering(result, out var addResults,
                        out var addDiagnosticInfos, out var removeResults,
                        out var removeDiagnosticInfos);
                    return NewSetTriggeringResponse(response, addResults, addDiagnosticInfos,
                        removeResults, removeDiagnosticInfos);
                }, TaskCreationOptions.DenyChildAttach);
#else
            return Task.Run(() => {
                var response = client.SetTriggering(requestHeader, subscriptionId,
                    triggeringItemId, linksToAdd, linksToRemove,
                    out var addResults, out var addDiagnosticInfos,
                    out var removeResults, out var removeDiagnosticInfos);
                return NewSetTriggeringResponse(response, addResults, addDiagnosticInfos,
                    removeResults, removeDiagnosticInfos);
            });
#endif
        }

        /// <summary>
        /// Async set monitoring mode service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="requestHeader"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="monitoringMode"></param>
        /// <param name="monitoredItemIds"></param>
        /// <returns></returns>
        public static Task<SetMonitoringModeResponse> SetMonitoringModeAsync(
            this SessionClient client, RequestHeader requestHeader, uint subscriptionId,
            MonitoringMode monitoringMode, UInt32Collection monitoredItemIds) {
#if !USE_TASK_RUN
            return Task.Factory.FromAsync(
                (callback, state) => client.BeginSetMonitoringMode(requestHeader,
                    subscriptionId, monitoringMode, monitoredItemIds, callback, state),
                result => {
                    var response = client.EndSetMonitoringMode(result, out var results,
                        out var diagnosticInfos);
                    return NewSetMonitoringModeResponse(response, results, diagnosticInfos);
                }, TaskCreationOptions.DenyChildAttach);
#else
            return Task.Run(() => {
                var response = client.SetMonitoringMode(requestHeader, subscriptionId,
                    monitoringMode, monitoredItemIds, out var results, out var diagnosticInfos);
                    return NewSetMonitoringModeResponse(response, results, diagnosticInfos);
            });
#endif
        }

        /// <summary>
        /// Async Delete monitored items service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="requestHeader"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="monitoredItemIds"></param>
        /// <returns></returns>
        public static Task<DeleteMonitoredItemsResponse> DeleteMonitoredItemsAsync(
            this SessionClient client, RequestHeader requestHeader, uint subscriptionId,
            UInt32Collection monitoredItemIds) {
#if !USE_TASK_RUN
            return Task.Factory.FromAsync(
                (callback, state) => client.BeginDeleteMonitoredItems(requestHeader,
                    subscriptionId, monitoredItemIds, callback, state),
                result => {
                    var response = client.EndDeleteMonitoredItems(result, out var results,
                        out var diagnosticInfos);
                    return NewDeleteMonitoredItemsResponse(response, results, diagnosticInfos);
                }, TaskCreationOptions.DenyChildAttach);
#else
            return Task.Run(() => {
                var response = client.DeleteMonitoredItems(requestHeader, subscriptionId,
                    monitoredItemIds, out var results, out var diagnosticInfos);
                    return NewDeleteMonitoredItemsResponse(response, results, diagnosticInfos);
            });
#endif
        }

        /// <summary>
        /// Async subscription creation service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="requestHeader"></param>
        /// <param name="requestedPublishingInterval"></param>
        /// <param name="requestedLifetimeCount"></param>
        /// <param name="requestedMaxKeepAliveCount"></param>
        /// <param name="maxNotificationsPerPublish"></param>
        /// <param name="publishingEnabled"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        public static Task<CreateSubscriptionResponse> CreateSubscriptionAsync(
            this SessionClient client, RequestHeader requestHeader,
            double requestedPublishingInterval, uint requestedLifetimeCount,
            uint requestedMaxKeepAliveCount, uint maxNotificationsPerPublish,
            bool publishingEnabled, byte priority) {
#if !USE_TASK_RUN
            return Task.Factory.FromAsync(
                (callback, state) => client.BeginCreateSubscription(requestHeader,
                    requestedPublishingInterval, requestedLifetimeCount, requestedMaxKeepAliveCount,
                    maxNotificationsPerPublish, publishingEnabled, priority, callback, state),
                result => {
                    var response = client.EndCreateSubscription(result, out var subsciptionId,
                        out var revisedPublishingInterval, out var revisedLifetimeCount,
                        out var revisedMaxKeepAliveCount);
                    return NewCreateSubscriptionResponse(response, subsciptionId,
                        revisedPublishingInterval, revisedLifetimeCount, revisedMaxKeepAliveCount);
                }, TaskCreationOptions.DenyChildAttach);
#else
            return Task.Run(() => {
                var response = client.CreateSubscription(requestHeader,
                    requestedPublishingInterval, requestedLifetimeCount, requestedMaxKeepAliveCount,
                    maxNotificationsPerPublish, publishingEnabled, priority,
                    out var subsciptionId, out var revisedPublishingInterval,
                    out var revisedLifetimeCount, out var revisedMaxKeepAliveCount);
                return NewCreateSubscriptionResponse(response, subsciptionId,
                    revisedPublishingInterval, revisedLifetimeCount, revisedMaxKeepAliveCount);
            });
#endif
        }

        /// <summary>
        /// Async subscription modification service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="requestHeader"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="requestedPublishingInterval"></param>
        /// <param name="requestedLifetimeCount"></param>
        /// <param name="requestedMaxKeepAliveCount"></param>
        /// <param name="maxNotificationsPerPublish"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        public static Task<ModifySubscriptionResponse> ModifySubscriptionAsync(
            this SessionClient client, RequestHeader requestHeader, uint subscriptionId,
            double requestedPublishingInterval, uint requestedLifetimeCount,
            uint requestedMaxKeepAliveCount, uint maxNotificationsPerPublish, byte priority) {
#if !USE_TASK_RUN
            return Task.Factory.FromAsync(
                (callback, state) => client.BeginModifySubscription(requestHeader, subscriptionId,
                    requestedPublishingInterval, requestedLifetimeCount, requestedMaxKeepAliveCount,
                    maxNotificationsPerPublish, priority, callback, state),
                result => {
                    var response = client.EndModifySubscription(result,
                        out var revisedPublishingInterval, out var revisedLifetimeCount,
                        out var revisedMaxKeepAliveCount);
                    return NewModifySubscriptionResponse(response,
                        revisedPublishingInterval, revisedLifetimeCount, revisedMaxKeepAliveCount);
                }, TaskCreationOptions.DenyChildAttach);
#else
            return Task.Run(() => {
                var response = client.ModifySubscription(requestHeader, subscriptionId,
                    requestedPublishingInterval, requestedLifetimeCount, requestedMaxKeepAliveCount,
                    maxNotificationsPerPublish, priority,
                    out var revisedPublishingInterval,
                    out var revisedLifetimeCount, out var revisedMaxKeepAliveCount);
                return NewModifySubscriptionResponse(response,
                    revisedPublishingInterval, revisedLifetimeCount, revisedMaxKeepAliveCount);
            });
#endif
        }

        /// <summary>
        /// Async publishing mode modification service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="requestHeader"></param>
        /// <param name="publishingEnabled"></param>
        /// <param name="subscriptionIds"></param>
        /// <returns></returns>
        public static Task<SetPublishingModeResponse> SetPublishingModeAsync(
            this SessionClient client, RequestHeader requestHeader, bool publishingEnabled,
            UInt32Collection subscriptionIds) {
#if !USE_TASK_RUN
            return Task.Factory.FromAsync(
                (callback, state) => client.BeginSetPublishingMode(requestHeader,
                    publishingEnabled, subscriptionIds, callback, state),
                result => {
                    var response = client.EndSetPublishingMode(result,
                        out var results, out var diagnosticInfos);
                    return NewSetPublishingModeResponse(response, results, diagnosticInfos);
                }, TaskCreationOptions.DenyChildAttach);
#else
            return Task.Run(() => {
                var response = client.SetPublishingMode(requestHeader, publishingEnabled,
                    subscriptionIds, out var results, out var diagnosticInfos);
                    return NewSetPublishingModeResponse(response, results, diagnosticInfos);
            });
#endif
        }

        /// <summary>
        /// Async publish service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="requestHeader"></param>
        /// <param name="subscriptionAcknowledgements"></param>
        /// <returns></returns>
        public static Task<PublishResponse> PublishAsync(
            this SessionClient client, RequestHeader requestHeader,
            SubscriptionAcknowledgementCollection subscriptionAcknowledgements) {
#if !USE_TASK_RUN
            return Task.Factory.FromAsync(
                (callback, state) => client.BeginPublish(requestHeader, subscriptionAcknowledgements,
                    callback, state),
                result => {
                    var response = client.EndPublish(result, out var subscriptionId,
                        out var availableSequenceNumbers, out var moreNotifications,
                        out var notificationMessage, out var results, out var diagnosticInfos);
                    return NewPublishResponse(response, subscriptionId, availableSequenceNumbers,
                        moreNotifications, notificationMessage, results, diagnosticInfos);
                }, TaskCreationOptions.DenyChildAttach);
#else
            return Task.Run(() => {
                var response = client.Publish(requestHeader, subscriptionAcknowledgements,
                    out var subscriptionId, out var availableSequenceNumbers,
                    out var moreNotifications, out var notificationMessage,
                    out var results, out var diagnosticInfos);
                return NewPublishResponse(response, subscriptionId, availableSequenceNumbers,
                    moreNotifications, notificationMessage, results, diagnosticInfos);
            });
#endif
        }
        /// <summary>
        /// Async republish service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="requestHeader"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="retransmitSequenceNumber"></param>
        /// <returns></returns>
        public static Task<RepublishResponse> RepublishAsync(this SessionClient client,
            RequestHeader requestHeader, uint subscriptionId, uint retransmitSequenceNumber) {
#if !USE_TASK_RUN
            return Task.Factory.FromAsync(
                (callback, state) => client.BeginRepublish(requestHeader, subscriptionId,
                    retransmitSequenceNumber, callback, state),
                result => {
                    var response = client.EndRepublish(result, out var notificationMessage);
                    return NewRepublishResponse(response, notificationMessage);
                }, TaskCreationOptions.DenyChildAttach);
#else
            return Task.Run(() => {
                var response = client.Republish(requestHeader, subscriptionId,
                    retransmitSequenceNumber, out var notificationMessage);
                return NewRepublishResponse(response, notificationMessage);
            });
#endif
        }

        /// <summary>
        /// Async subscription transfer service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="requestHeader"></param>
        /// <param name="subscriptionIds"></param>
        /// <param name="sendInitialValues"></param>
        /// <returns></returns>
        public static Task<TransferSubscriptionsResponse> TransferSubscriptionsAsync(
            this SessionClient client, RequestHeader requestHeader, UInt32Collection subscriptionIds,
            bool sendInitialValues) {
#if !USE_TASK_RUN
            return Task.Factory.FromAsync(
                (callback, state) => client.BeginTransferSubscriptions(requestHeader, subscriptionIds,
                    sendInitialValues, callback, state),
                result => {
                    var response = client.EndTransferSubscriptions(result, out var results,
                        out var diagnosticInfos);
                    return NewTransferSubscriptionsResponse(response, results, diagnosticInfos);
                }, TaskCreationOptions.DenyChildAttach);
#else
            return Task.Run(() => {
                var response = client.TransferSubscriptions(requestHeader, subscriptionIds,
                    sendInitialValues, out var results, out var diagnosticInfos);
                    return NewTransferSubscriptionsResponse(response, results, diagnosticInfos);
            });
#endif
        }

        /// <summary>
        /// Async subscription modification service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="requestHeader"></param>
        /// <param name="subscriptionIds"></param>
        /// <returns></returns>
        public static Task<DeleteSubscriptionsResponse> DeleteSubscriptionsAsync(
            this SessionClient client, RequestHeader requestHeader, UInt32Collection subscriptionIds) {
#if !USE_TASK_RUN
            return Task.Factory.FromAsync(
                (callback, state) => client.BeginDeleteSubscriptions(requestHeader, subscriptionIds,
                    callback, state),
                result => {
                    var response = client.EndDeleteSubscriptions(result,
                        out var results, out var diagnosticInfos);
                    return NewDeleteSubscriptionsResponse(response, results, diagnosticInfos);
                }, TaskCreationOptions.DenyChildAttach);
#else
            return Task.Run(() => {
                var response = client.DeleteSubscriptions(requestHeader, subscriptionIds,
                    out var results, out var diagnosticInfos);
                return NewDeleteSubscriptionsResponse(response, results, diagnosticInfos);
            });
#endif
        }

        /// <summary>
        /// Validates responses
        /// </summary>
        /// <param name="results"></param>
        /// <param name="diagnostics"></param>
        public static void Validate<T>(IEnumerable<T> results,
            DiagnosticInfoCollection diagnostics) {
            Validate<T, object>(results, diagnostics, null);
        }

        /// <summary>
        /// Validates responses against requests
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="results"></param>
        /// <param name="diagnostics"></param>
        /// <param name="requested"></param>
        public static void Validate<T, R>(IEnumerable<T> results,
            DiagnosticInfoCollection diagnostics, IEnumerable<R> requested) {
            var resultsWithStatus = results?.ToList();
            if (resultsWithStatus == null || (resultsWithStatus.Count == 0 &&
                diagnostics.Count == 0)) {
                throw new ServiceResultException(StatusCodes.BadUnexpectedError,
                    "The server returned no results or diagnostics information.");
            }
            // Throw on bad responses.
            var expected = requested?.Count() ?? 1;
            if (resultsWithStatus.Count != expected) {
                throw new ServiceResultException(StatusCodes.BadUnexpectedError,
                    "The server returned a list without the expected number of elements.");
            }
            if (diagnostics != null && diagnostics.Count != 0 && diagnostics.Count != expected) {
                throw new ServiceResultException(StatusCodes.BadUnexpectedError,
                    "The server forgot to fill in the DiagnosticInfos array correctly.");
            }
        }

        /// <summary>
        /// Delete subscriptions response constructor
        /// </summary>
        /// <param name="response"></param>
        /// <param name="results"></param>
        /// <param name="diagnosticInfos"></param>
        /// <returns></returns>
        private static DeleteSubscriptionsResponse NewDeleteSubscriptionsResponse(
            ResponseHeader response, StatusCodeCollection results,
            DiagnosticInfoCollection diagnosticInfos) {
            return new DeleteSubscriptionsResponse {
                DiagnosticInfos = diagnosticInfos,
                ResponseHeader = response,
                Results = results
            };
        }


        /// <summary>
        /// Async transfer subscription response constructor
        /// </summary>
        /// <param name="response"></param>
        /// <param name="results"></param>
        /// <param name="diagnosticInfos"></param>
        /// <returns></returns>
        private static TransferSubscriptionsResponse NewTransferSubscriptionsResponse(
            ResponseHeader response, TransferResultCollection results,
            DiagnosticInfoCollection diagnosticInfos) {
            return new TransferSubscriptionsResponse {
                DiagnosticInfos = diagnosticInfos,
                ResponseHeader = response,
                Results = results
            };
        }

        /// <summary>
        /// Async subscription modification service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="requestHeader"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="requestedPublishingInterval"></param>
        /// <param name="requestedLifetimeCount"></param>
        /// <param name="requestedMaxKeepAliveCount"></param>
        /// <param name="maxNotificationsPerPublish"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        public static Task<ModifySubscriptionResponse> DeleteSubscriptionAsync(
            this SessionClient client, RequestHeader requestHeader, uint subscriptionId,
            double requestedPublishingInterval, uint requestedLifetimeCount,
            uint requestedMaxKeepAliveCount, uint maxNotificationsPerPublish, byte priority) {
#if !USE_TASK_RUN
            return Task.Factory.FromAsync(
                (callback, state) => client.BeginModifySubscription(requestHeader, subscriptionId,
                    requestedPublishingInterval, requestedLifetimeCount, requestedMaxKeepAliveCount,
                    maxNotificationsPerPublish, priority, callback, state),
                result => {
                    var response = client.EndModifySubscription(result,
                        out var revisedPublishingInterval, out var revisedLifetimeCount,
                        out var revisedMaxKeepAliveCount);
                    return NewModifySubscriptionResponse(response,
                        revisedPublishingInterval, revisedLifetimeCount, revisedMaxKeepAliveCount);
                }, TaskCreationOptions.DenyChildAttach);
#else
            return Task.Run(() => {
                var response = client.ModifySubscription(requestHeader, subscriptionId,
                    requestedPublishingInterval, requestedLifetimeCount, requestedMaxKeepAliveCount,
                    maxNotificationsPerPublish, priority,
                    out var revisedPublishingInterval,
                    out var revisedLifetimeCount, out var revisedMaxKeepAliveCount);
                return NewModifySubscriptionResponse(response,
                    revisedPublishingInterval, revisedLifetimeCount, revisedMaxKeepAliveCount);
            });
#endif
        }

        /// <summary>
        /// Republish response constructor
        /// </summary>
        /// <param name="response"></param>
        /// <param name="notificationMessage"></param>
        /// <returns></returns>
        private static RepublishResponse NewRepublishResponse(ResponseHeader response,
            NotificationMessage notificationMessage) {
            return new RepublishResponse {
                ResponseHeader = response,
                NotificationMessage = notificationMessage
            };
        }

        /// <summary>
        /// Publish response constructor
        /// </summary>
        /// <param name="response"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="availableSequenceNumbers"></param>
        /// <param name="moreNotifications"></param>
        /// <param name="notificationMessage"></param>
        /// <param name="results"></param>
        /// <param name="diagnosticInfos"></param>
        /// <returns></returns>
        private static PublishResponse NewPublishResponse(ResponseHeader response,
            uint subscriptionId, UInt32Collection availableSequenceNumbers, bool moreNotifications,
            NotificationMessage notificationMessage, StatusCodeCollection results,
            DiagnosticInfoCollection diagnosticInfos) {
            return new PublishResponse {
                DiagnosticInfos = diagnosticInfos,
                Results = results,
                AvailableSequenceNumbers = availableSequenceNumbers,
                MoreNotifications = moreNotifications,
                NotificationMessage = notificationMessage,
                ResponseHeader = response,
                SubscriptionId = subscriptionId
            };
        }

        /// <summary>
        /// Set publishing mode response constructor
        /// </summary>
        /// <param name="response"></param>
        /// <param name="results"></param>
        /// <param name="diagnosticInfos"></param>
        /// <returns></returns>
        private static SetPublishingModeResponse NewSetPublishingModeResponse(
            ResponseHeader response, StatusCodeCollection results,
            DiagnosticInfoCollection diagnosticInfos) {
            return new SetPublishingModeResponse {
                DiagnosticInfos = diagnosticInfos,
                ResponseHeader = response,
                Results = results
            };
        }

        /// <summary>
        /// Modify Subscription response constructor
        /// </summary>
        /// <param name="response"></param>
        /// <param name="revisedPublishingInterval"></param>
        /// <param name="revisedLifetimeCount"></param>
        /// <param name="revisedMaxKeepAliveCount"></param>
        /// <returns></returns>
        private static ModifySubscriptionResponse NewModifySubscriptionResponse(
            ResponseHeader response, double revisedPublishingInterval,
            uint revisedLifetimeCount, uint revisedMaxKeepAliveCount) {
            return new ModifySubscriptionResponse {
                ResponseHeader = response,
                RevisedLifetimeCount = revisedLifetimeCount,
                RevisedMaxKeepAliveCount = revisedMaxKeepAliveCount,
                RevisedPublishingInterval = revisedPublishingInterval
            };
        }

        /// <summary>
        /// Create Subscription response constructor
        /// </summary>
        /// <param name="response"></param>
        /// <param name="subsciptionId"></param>
        /// <param name="revisedPublishingInterval"></param>
        /// <param name="revisedLifetimeCount"></param>
        /// <param name="revisedMaxKeepAliveCount"></param>
        /// <returns></returns>
        private static CreateSubscriptionResponse NewCreateSubscriptionResponse(
            ResponseHeader response, uint subsciptionId, double revisedPublishingInterval,
            uint revisedLifetimeCount, uint revisedMaxKeepAliveCount) {
            return new CreateSubscriptionResponse {
                ResponseHeader = response,
                RevisedLifetimeCount = revisedLifetimeCount,
                RevisedMaxKeepAliveCount = revisedMaxKeepAliveCount,
                RevisedPublishingInterval = revisedPublishingInterval,
                SubscriptionId = subsciptionId
            };
        }

        /// <summary>
        /// Delete monitored items response constructor
        /// </summary>
        /// <param name="response"></param>
        /// <param name="results"></param>
        /// <param name="diagnosticInfos"></param>
        /// <returns></returns>
        private static DeleteMonitoredItemsResponse NewDeleteMonitoredItemsResponse(
            ResponseHeader response, StatusCodeCollection results,
            DiagnosticInfoCollection diagnosticInfos) {
            return new DeleteMonitoredItemsResponse {
                DiagnosticInfos = diagnosticInfos,
                ResponseHeader = response,
                Results = results
            };
        }

        /// <summary>
        /// Set monitoring mode response constructor
        /// </summary>
        /// <param name="response"></param>
        /// <param name="results"></param>
        /// <param name="diagnosticInfos"></param>
        /// <returns></returns>
        private static SetMonitoringModeResponse NewSetMonitoringModeResponse(
            ResponseHeader response, StatusCodeCollection results,
            DiagnosticInfoCollection diagnosticInfos) {
            return new SetMonitoringModeResponse {
                DiagnosticInfos = diagnosticInfos,
                ResponseHeader = response,
                Results = results
            };
        }

        /// <summary>
        /// Modify monitored items response constructor
        /// </summary>
        /// <param name="response"></param>
        /// <param name="results"></param>
        /// <param name="diagnosticInfos"></param>
        /// <returns></returns>
        private static ModifyMonitoredItemsResponse NewModifyMonitoredItemsResponse(
            ResponseHeader response, MonitoredItemModifyResultCollection results,
            DiagnosticInfoCollection diagnosticInfos) {
            return new ModifyMonitoredItemsResponse {
                Results = results,
                ResponseHeader = response,
                DiagnosticInfos = diagnosticInfos
            };
        }

        /// <summary>
        /// Create set triggering response constructor
        /// </summary>
        /// <param name="response"></param>
        /// <param name="addResults"></param>
        /// <param name="addDiagnosticInfos"></param>
        /// <param name="removeResults"></param>
        /// <param name="removeDiagnosticInfos"></param>
        /// <returns></returns>
        private static SetTriggeringResponse NewSetTriggeringResponse(ResponseHeader response,
            StatusCodeCollection addResults, DiagnosticInfoCollection addDiagnosticInfos,
            StatusCodeCollection removeResults, DiagnosticInfoCollection removeDiagnosticInfos) {
            return new SetTriggeringResponse {
                ResponseHeader = response,
                AddDiagnosticInfos = addDiagnosticInfos,
                AddResults = addResults,
                RemoveDiagnosticInfos = removeDiagnosticInfos,
                RemoveResults = removeResults
            };
        }

        /// <summary>
        /// Create monitored items response constructor
        /// </summary>
        /// <param name="response"></param>
        /// <param name="results"></param>
        /// <param name="diagnosticInfos"></param>
        /// <returns></returns>
        private static CreateMonitoredItemsResponse NewCreateMonitoredItemsResponse(
            ResponseHeader response, MonitoredItemCreateResultCollection results,
            DiagnosticInfoCollection diagnosticInfos) {
            return new CreateMonitoredItemsResponse {
                DiagnosticInfos = diagnosticInfos,
                ResponseHeader = response,
                Results = results
            };
        }

        /// <summary>
        /// History update response constructor
        /// </summary>
        /// <param name="response"></param>
        /// <param name="results"></param>
        /// <param name="diagnosticInfos"></param>
        /// <returns></returns>
        private static HistoryUpdateResponse NewHistoryUpdateResponse(
            ResponseHeader response, HistoryUpdateResultCollection results,
            DiagnosticInfoCollection diagnosticInfos) {
            return new HistoryUpdateResponse {
                Results = results,
                DiagnosticInfos = diagnosticInfos,
                ResponseHeader = response
            };
        }

        /// <summary>
        /// History read response constructor
        /// </summary>
        /// <param name="response"></param>
        /// <param name="results"></param>
        /// <param name="diagnosticInfos"></param>
        /// <returns></returns>
        private static HistoryReadResponse NewHistoryReadResponse(
            ResponseHeader response, HistoryReadResultCollection results,
            DiagnosticInfoCollection diagnosticInfos) {
            return new HistoryReadResponse {
                DiagnosticInfos = diagnosticInfos,
                ResponseHeader = response,
                Results = results
            };
        }

        /// <summary>
        /// Cancel response constructor
        /// </summary>
        /// <param name="response"></param>
        /// <param name="cancelCount"></param>
        /// <returns></returns>
        private static CancelResponse NewCancelResponse(ResponseHeader response,
            uint cancelCount) {
            return new CancelResponse {
                CancelCount = cancelCount,
                ResponseHeader = response
            };
        }

        /// <summary>
        /// Call response constructor
        /// </summary>
        /// <param name="response"></param>
        /// <param name="results"></param>
        /// <param name="diagnosticInfos"></param>
        /// <returns></returns>
        private static CallResponse NewCallResponse(ResponseHeader response,
            CallMethodResultCollection results, DiagnosticInfoCollection diagnosticInfos) {
            return new CallResponse {
                Results = results,
                ResponseHeader = response,
                DiagnosticInfos = diagnosticInfos
            };
        }

        /// <summary>
        /// Write response constructor
        /// </summary>
        /// <param name="response"></param>
        /// <param name="results"></param>
        /// <param name="diagnosticInfos"></param>
        /// <returns></returns>
        private static WriteResponse NewWriteResponse(ResponseHeader response,
            StatusCodeCollection results, DiagnosticInfoCollection diagnosticInfos) {
            return new WriteResponse {
                Results = results,
                ResponseHeader = response,
                DiagnosticInfos = diagnosticInfos
            };
        }

        /// <summary>
        /// Translate browse paths to node ids response constructor
        /// </summary>
        /// <param name="response"></param>
        /// <param name="results"></param>
        /// <param name="diagnosticInfos"></param>
        /// <returns></returns>
        private static TranslateBrowsePathsToNodeIdsResponse NewTranslateBrowsePathsToNodeIdsResponse(
            ResponseHeader response, BrowsePathResultCollection results,
            DiagnosticInfoCollection diagnosticInfos) {
            return new TranslateBrowsePathsToNodeIdsResponse {
                DiagnosticInfos = diagnosticInfos,
                ResponseHeader = response,
                Results = results
            };
        }

        /// <summary>
        /// Read response constructor
        /// </summary>
        /// <param name="response"></param>
        /// <param name="results"></param>
        /// <param name="diagnosticInfos"></param>
        /// <returns></returns>
        private static ReadResponse NewReadResponse(ResponseHeader response,
            DataValueCollection results, DiagnosticInfoCollection diagnosticInfos) {
            return new ReadResponse {
                DiagnosticInfos = diagnosticInfos,
                ResponseHeader = response,
                Results = results
            };
        }

        /// <summary>
        /// Browse response constructor
        /// </summary>
        /// <param name="response"></param>
        /// <param name="results"></param>
        /// <param name="diagnosticInfos"></param>
        /// <returns></returns>
        private static BrowseResponse NewBrowseResponse(ResponseHeader response,
            BrowseResultCollection results, DiagnosticInfoCollection diagnosticInfos) {
            return new BrowseResponse {
                DiagnosticInfos = diagnosticInfos,
                ResponseHeader = response,
                Results = results
            };
        }

        /// <summary>
        /// Activate client response constructor
        /// </summary>
        /// <param name="response"></param>
        /// <param name="serverNonce"></param>
        /// <param name="results"></param>
        /// <param name="diagnosticInfos"></param>
        /// <returns></returns>
        private static ActivateSessionResponse NewActivateSessionResponse(
            ResponseHeader response, byte[] serverNonce, StatusCodeCollection results,
            DiagnosticInfoCollection diagnosticInfos) {
            return new ActivateSessionResponse {
                ResponseHeader = response,
                DiagnosticInfos = diagnosticInfos,
                Results = results,
                ServerNonce = serverNonce
            };
        }

        /// <summary>
        /// Create client response constructor
        /// </summary>
        /// <param name="response"></param>
        /// <param name="sessionId"></param>
        /// <param name="authenticationToken"></param>
        /// <param name="revisedSessionTimeout"></param>
        /// <param name="serverNonce"></param>
        /// <param name="serverCertificate"></param>
        /// <param name="serverEndpoints"></param>
        /// <param name="serverSoftwareCertificates"></param>
        /// <param name="serverSignature"></param>
        /// <param name="maxRequestMessageSize"></param>
        /// <returns></returns>
        private static CreateSessionResponse NewCreateSessionResponse(ResponseHeader response,
            NodeId sessionId, NodeId authenticationToken, double revisedSessionTimeout,
            byte[] serverNonce, byte[] serverCertificate, EndpointDescriptionCollection serverEndpoints,
            SignedSoftwareCertificateCollection serverSoftwareCertificates,
            SignatureData serverSignature, uint maxRequestMessageSize) {
            return new CreateSessionResponse {
                ResponseHeader = response,
                SessionId = sessionId,
                AuthenticationToken = authenticationToken,
                RevisedSessionTimeout = revisedSessionTimeout,
                ServerNonce = serverNonce,
                ServerCertificate = serverCertificate,
                ServerEndpoints = serverEndpoints,
                ServerSoftwareCertificates = serverSoftwareCertificates,
                ServerSignature = serverSignature,
                MaxRequestMessageSize = maxRequestMessageSize
            };
        }
    }
}
