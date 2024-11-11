// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client.Obsolete
{
    using Opc.Ua;
    using System;
    using System.Diagnostics;

    /// <summary>
    /// The client side interface with support for batching according to operation limits.
    /// </summary>
    public class SessionClient : Opc.Ua.SessionClient
    {
        /// <summary>
        /// Intializes the object with a channel and default operation limits.
        /// </summary>
        /// <param name="channel"></param>
        public SessionClient(ITransportChannel channel)
            : base(channel)
        {
        }

        /// <inheritdoc/>
        public override StatusCode Close()
        {
            throw NotSupported(nameof(Close));
        }

        /// <inheritdoc/>
        public override ResponseHeader CreateSession(RequestHeader? requestHeader,
            ApplicationDescription clientDescription, string serverUri, string endpointUrl,
            string sessionName, byte[] clientNonce, byte[] clientCertificate,
            double requestedSessionTimeout, uint maxResponseMessageSize, out NodeId sessionId,
            out NodeId authenticationToken, out double revisedSessionTimeout, out byte[] serverNonce,
            out byte[] serverCertificate, out EndpointDescriptionCollection serverEndpoints,
            out SignedSoftwareCertificateCollection serverSoftwareCertificates,
            out SignatureData serverSignature, out uint maxRequestMessageSize)
        {
            throw NotSupported(nameof(CreateSession));
        }

        /// <inheritdoc/>
        public override IAsyncResult BeginCreateSession(RequestHeader? requestHeader,
            ApplicationDescription clientDescription, string serverUri, string endpointUrl,
            string sessionName, byte[] clientNonce, byte[] clientCertificate, double requestedSessionTimeout,
            uint maxResponseMessageSize, AsyncCallback callback, object asyncState)
        {
            throw NotSupported(nameof(BeginCreateSession));
        }

        /// <inheritdoc/>
        public override ResponseHeader EndCreateSession(IAsyncResult result,
            out NodeId sessionId, out NodeId authenticationToken,
            out double revisedSessionTimeout, out byte[] serverNonce,
            out byte[] serverCertificate, out EndpointDescriptionCollection serverEndpoints,
            out SignedSoftwareCertificateCollection serverSoftwareCertificates,
            out SignatureData serverSignature,
            out uint maxRequestMessageSize)
        {
            throw NotSupported(nameof(EndCreateSession));
        }

        /// <inheritdoc/>
        public override ResponseHeader ActivateSession(
            RequestHeader? requestHeader, SignatureData clientSignature,
            SignedSoftwareCertificateCollection clientSoftwareCertificates,
            StringCollection localeIds, ExtensionObject userIdentityToken,
            SignatureData userTokenSignature, out byte[] serverNonce,
            out StatusCodeCollection results, out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(ActivateSession));
        }

        /// <inheritdoc/>
        public override IAsyncResult BeginActivateSession(RequestHeader? requestHeader,
            SignatureData clientSignature, SignedSoftwareCertificateCollection? clientSoftwareCertificates,
            StringCollection localeIds, ExtensionObject userIdentityToken, SignatureData userTokenSignature,
            AsyncCallback? callback, object? asyncState)
        {
            throw NotSupported(nameof(BeginActivateSession));
        }

        /// <inheritdoc/>
        public override ResponseHeader EndActivateSession(IAsyncResult result,
            out byte[] serverNonce, out StatusCodeCollection results, out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(EndActivateSession));
        }

        /// <inheritdoc/>
        public override ResponseHeader CloseSession(RequestHeader? requestHeader, bool deleteSubscriptions)
        {
            throw NotSupported(nameof(CloseSession));
        }

        /// <inheritdoc/>
        public override IAsyncResult BeginCloseSession(RequestHeader? requestHeader,
            bool deleteSubscriptions, AsyncCallback callback, object asyncState)
        {
            throw NotSupported(nameof(BeginCloseSession));
        }

        /// <inheritdoc/>
        public override ResponseHeader EndCloseSession(IAsyncResult result)
        {
            throw NotSupported(nameof(EndCloseSession));
        }

        /// <inheritdoc/>
        public override ResponseHeader Cancel(RequestHeader? requestHeader,
            uint requestHandle, out uint cancelCount)
        {
            throw NotSupported(nameof(Cancel));
        }

        /// <inheritdoc/>
        public override IAsyncResult BeginCancel(RequestHeader? requestHeader,
            uint requestHandle, AsyncCallback callback, object asyncState)
        {
            throw NotSupported(nameof(BeginCancel));
        }

        /// <inheritdoc/>
        public override ResponseHeader EndCancel(IAsyncResult result, out uint cancelCount)
        {
            throw NotSupported(nameof(EndCancel));
        }

        /// <inheritdoc/>
        public override ResponseHeader AddNodes(RequestHeader? requestHeader,
            AddNodesItemCollection nodesToAdd, out AddNodesResultCollection results,
            out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(AddNodes));
        }

        /// <inheritdoc/>
        public override IAsyncResult BeginAddNodes(RequestHeader? requestHeader,
            AddNodesItemCollection nodesToAdd, AsyncCallback callback, object asyncState)
        {
            throw NotSupported(nameof(BeginAddNodes));
        }

        /// <inheritdoc/>
        public override ResponseHeader EndAddNodes(IAsyncResult result, out AddNodesResultCollection results,
            out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(EndAddNodes));
        }

        /// <inheritdoc/>
        public override ResponseHeader AddReferences(RequestHeader? requestHeader,
            AddReferencesItemCollection referencesToAdd, out StatusCodeCollection results,
            out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(AddReferences));
        }

        /// <inheritdoc/>
        public override IAsyncResult BeginAddReferences(RequestHeader? requestHeader,
            AddReferencesItemCollection referencesToAdd, AsyncCallback callback, object asyncState)
        {
            throw NotSupported(nameof(BeginAddReferences));
        }

        /// <inheritdoc/>
        public override ResponseHeader EndAddReferences(IAsyncResult result,
            out StatusCodeCollection results, out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(EndAddReferences));
        }

        /// <inheritdoc/>
        public override ResponseHeader DeleteNodes(RequestHeader? requestHeader,
            DeleteNodesItemCollection nodesToDelete, out StatusCodeCollection results,
            out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(DeleteNodes));
        }

        /// <inheritdoc/>
        public override IAsyncResult BeginDeleteNodes(RequestHeader? requestHeader,
            DeleteNodesItemCollection nodesToDelete, AsyncCallback callback, object asyncState)
        {
            throw NotSupported(nameof(BeginDeleteNodes));
        }

        /// <inheritdoc/>
        public override ResponseHeader EndDeleteNodes(IAsyncResult result,
            out StatusCodeCollection results, out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(EndDeleteNodes));
        }

        /// <inheritdoc/>
        public override ResponseHeader DeleteReferences(RequestHeader? requestHeader,
            DeleteReferencesItemCollection referencesToDelete, out StatusCodeCollection results,
            out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(DeleteReferences));
        }

        /// <inheritdoc/>
        public override IAsyncResult BeginDeleteReferences(RequestHeader? requestHeader,
            DeleteReferencesItemCollection referencesToDelete, AsyncCallback callback,
            object asyncState)
        {
            throw NotSupported(nameof(BeginDeleteReferences));
        }

        /// <inheritdoc/>
        public override ResponseHeader EndDeleteReferences(IAsyncResult result,
            out StatusCodeCollection results, out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(EndDeleteReferences));
        }

        /// <inheritdoc/>
        public override ResponseHeader Browse(RequestHeader? requestHeader, ViewDescription view,
            uint requestedMaxReferencesPerNode, BrowseDescriptionCollection nodesToBrowse,
            out BrowseResultCollection results, out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(Browse));
        }

        /// <inheritdoc/>
        public override IAsyncResult BeginBrowse(RequestHeader? requestHeader,
            ViewDescription view, uint requestedMaxReferencesPerNode,
            BrowseDescriptionCollection nodesToBrowse, AsyncCallback callback, object asyncState)
        {
            throw NotSupported(nameof(BeginBrowse));
        }

        /// <inheritdoc/>
        public override ResponseHeader EndBrowse(IAsyncResult result,
            out BrowseResultCollection results, out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(EndBrowse));
        }

        /// <inheritdoc/>
        public override ResponseHeader BrowseNext(RequestHeader? requestHeader,
            bool releaseContinuationPoints, ByteStringCollection continuationPoints,
            out BrowseResultCollection results, out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(BrowseNext));
        }

        /// <inheritdoc/>
        public override IAsyncResult BeginBrowseNext(RequestHeader? requestHeader,
            bool releaseContinuationPoints, ByteStringCollection continuationPoints,
            AsyncCallback callback, object asyncState)
        {
            throw NotSupported(nameof(BeginBrowseNext));
        }

        /// <inheritdoc/>
        public override ResponseHeader EndBrowseNext(IAsyncResult result,
            out BrowseResultCollection results, out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(EndBrowseNext));
        }

        /// <inheritdoc/>
        public override ResponseHeader TranslateBrowsePathsToNodeIds(RequestHeader? requestHeader,
            BrowsePathCollection browsePaths, out BrowsePathResultCollection results,
            out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(TranslateBrowsePathsToNodeIds));
        }

        /// <inheritdoc/>
        public override IAsyncResult BeginTranslateBrowsePathsToNodeIds(RequestHeader? requestHeader,
            BrowsePathCollection browsePaths, AsyncCallback callback, object asyncState)
        {
            throw NotSupported(nameof(BeginTranslateBrowsePathsToNodeIds));
        }

        /// <inheritdoc/>
        public override ResponseHeader EndTranslateBrowsePathsToNodeIds(IAsyncResult result,
            out BrowsePathResultCollection results, out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(EndTranslateBrowsePathsToNodeIds));
        }

        /// <inheritdoc/>
        public override ResponseHeader RegisterNodes(RequestHeader? requestHeader,
            NodeIdCollection nodesToRegister, out NodeIdCollection registeredNodeIds)
        {
            throw NotSupported(nameof(RegisterNodes));
        }

        /// <inheritdoc/>
        public override IAsyncResult BeginRegisterNodes(RequestHeader? requestHeader,
            NodeIdCollection nodesToRegister, AsyncCallback callback, object asyncState)
        {
            throw NotSupported(nameof(BeginRegisterNodes));
        }

        /// <inheritdoc/>
        public override ResponseHeader EndRegisterNodes(IAsyncResult result,
            out NodeIdCollection registeredNodeIds)
        {
            throw NotSupported(nameof(EndRegisterNodes));
        }

        /// <inheritdoc/>
        public override ResponseHeader UnregisterNodes(RequestHeader? requestHeader,
            NodeIdCollection nodesToUnregister)
        {
            throw NotSupported(nameof(UnregisterNodes));
        }

        /// <inheritdoc/>
        public override IAsyncResult BeginUnregisterNodes(RequestHeader? requestHeader,
            NodeIdCollection nodesToUnregister, AsyncCallback callback, object asyncState)
        {
            throw NotSupported(nameof(BeginUnregisterNodes));
        }

        /// <inheritdoc/>
        public override ResponseHeader EndUnregisterNodes(IAsyncResult result)
        {
            throw NotSupported(nameof(EndUnregisterNodes));
        }

        /// <inheritdoc/>
        public override ResponseHeader QueryFirst(RequestHeader? requestHeader,
            ViewDescription view, NodeTypeDescriptionCollection nodeTypes,
            ContentFilter filter, uint maxDataSetsToReturn, uint maxReferencesToReturn,
            out QueryDataSetCollection queryDataSets, out byte[] continuationPoint,
            out ParsingResultCollection parsingResults, out DiagnosticInfoCollection diagnosticInfos,
            out ContentFilterResult filterResult)
        {
            throw NotSupported(nameof(QueryFirst));
        }

        /// <inheritdoc/>
        public override IAsyncResult BeginQueryFirst(RequestHeader? requestHeader,
            ViewDescription view, NodeTypeDescriptionCollection nodeTypes, ContentFilter filter,
            uint maxDataSetsToReturn, uint maxReferencesToReturn, AsyncCallback callback,
            object asyncState)
        {
            throw NotSupported(nameof(BeginQueryFirst));
        }

        /// <inheritdoc/>
        public override ResponseHeader EndQueryFirst(IAsyncResult result,
            out QueryDataSetCollection queryDataSets, out byte[] continuationPoint,
            out ParsingResultCollection parsingResults, out DiagnosticInfoCollection diagnosticInfos,
            out ContentFilterResult filterResult)
        {
            throw NotSupported(nameof(EndQueryFirst));
        }

        /// <inheritdoc/>
        public override ResponseHeader QueryNext(RequestHeader? requestHeader,
            bool releaseContinuationPoint, byte[] continuationPoint,
            out QueryDataSetCollection queryDataSets, out byte[] revisedContinuationPoint)
        {
            throw NotSupported(nameof(QueryNext));
        }

        /// <inheritdoc/>
        public override IAsyncResult BeginQueryNext(RequestHeader? requestHeader,
            bool releaseContinuationPoint, byte[] continuationPoint, AsyncCallback callback,
            object asyncState)
        {
            throw NotSupported(nameof(BeginQueryNext));
        }

        /// <inheritdoc/>
        public override ResponseHeader EndQueryNext(IAsyncResult result,
            out QueryDataSetCollection queryDataSets, out byte[] revisedContinuationPoint)
        {
            throw NotSupported(nameof(EndQueryNext));
        }

        /// <inheritdoc/>
        public override ResponseHeader HistoryRead(RequestHeader? requestHeader,
            ExtensionObject historyReadDetails, TimestampsToReturn timestampsToReturn,
            bool releaseContinuationPoints, HistoryReadValueIdCollection nodesToRead,
            out HistoryReadResultCollection results, out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(HistoryRead));
        }

        /// <inheritdoc/>
        public override IAsyncResult BeginHistoryRead(RequestHeader? requestHeader,
            ExtensionObject historyReadDetails, TimestampsToReturn timestampsToReturn,
            bool releaseContinuationPoints, HistoryReadValueIdCollection nodesToRead,
            AsyncCallback callback, object asyncState)
        {
            throw NotSupported(nameof(BeginHistoryRead));
        }

        /// <inheritdoc/>
        public override ResponseHeader EndHistoryRead(IAsyncResult result,
            out HistoryReadResultCollection results, out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(EndHistoryRead));
        }

        /// <inheritdoc/>
        public override ResponseHeader Write(RequestHeader? requestHeader,
            WriteValueCollection nodesToWrite, out StatusCodeCollection results,
            out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(Write));
        }

        /// <inheritdoc/>
        public override IAsyncResult BeginWrite(RequestHeader? requestHeader,
            WriteValueCollection nodesToWrite, AsyncCallback callback, object asyncState)
        {
            throw NotSupported(nameof(BeginWrite));
        }

        /// <inheritdoc/>
        public override ResponseHeader EndWrite(IAsyncResult result,
            out StatusCodeCollection results, out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(EndWrite));
        }

        /// <inheritdoc/>
        public override ResponseHeader HistoryUpdate(RequestHeader? requestHeader,
            ExtensionObjectCollection historyUpdateDetails, out HistoryUpdateResultCollection results,
            out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(HistoryUpdate));
        }

        /// <inheritdoc/>
        public override IAsyncResult BeginHistoryUpdate(RequestHeader? requestHeader,
            ExtensionObjectCollection historyUpdateDetails, AsyncCallback callback, object asyncState)
        {
            throw NotSupported(nameof(BeginHistoryUpdate));
        }

        /// <inheritdoc/>
        public override ResponseHeader EndHistoryUpdate(IAsyncResult result,
            out HistoryUpdateResultCollection results, out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(EndHistoryUpdate));
        }

        /// <inheritdoc/>
        public override ResponseHeader Call(RequestHeader? requestHeader,
            CallMethodRequestCollection methodsToCall, out CallMethodResultCollection results,
            out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(Call));
        }

        /// <inheritdoc/>
        public override IAsyncResult BeginCall(RequestHeader? requestHeader,
            CallMethodRequestCollection methodsToCall, AsyncCallback callback, object asyncState)
        {
            throw NotSupported(nameof(BeginCall));
        }

        /// <inheritdoc/>
        public override ResponseHeader EndCall(IAsyncResult result,
            out CallMethodResultCollection results, out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(EndCall));
        }

        /// <inheritdoc/>
        public override ResponseHeader CreateMonitoredItems(RequestHeader? requestHeader,
            uint subscriptionId, TimestampsToReturn timestampsToReturn,
            MonitoredItemCreateRequestCollection itemsToCreate,
            out MonitoredItemCreateResultCollection results,
            out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(CreateMonitoredItems));
        }

        /// <inheritdoc/>
        public override IAsyncResult BeginCreateMonitoredItems(RequestHeader? requestHeader,
            uint subscriptionId, TimestampsToReturn timestampsToReturn,
            MonitoredItemCreateRequestCollection itemsToCreate, AsyncCallback callback,
            object asyncState)
        {
            throw NotSupported(nameof(BeginCreateMonitoredItems));
        }

        /// <inheritdoc/>
        public override ResponseHeader EndCreateMonitoredItems(IAsyncResult result,
            out MonitoredItemCreateResultCollection results, out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(EndCreateMonitoredItems));
        }

        /// <inheritdoc/>
        public override ResponseHeader ModifyMonitoredItems(RequestHeader? requestHeader,
            uint subscriptionId, TimestampsToReturn timestampsToReturn,
            MonitoredItemModifyRequestCollection itemsToModify,
            out MonitoredItemModifyResultCollection results, out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(ModifyMonitoredItems));
        }

        /// <inheritdoc/>
        public override IAsyncResult BeginModifyMonitoredItems(RequestHeader? requestHeader,
            uint subscriptionId, TimestampsToReturn timestampsToReturn,
            MonitoredItemModifyRequestCollection itemsToModify, AsyncCallback callback,
            object asyncState)
        {
            throw NotSupported(nameof(BeginModifyMonitoredItems));
        }

        /// <inheritdoc/>
        public override ResponseHeader EndModifyMonitoredItems(IAsyncResult result,
            out MonitoredItemModifyResultCollection results,
            out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(EndModifyMonitoredItems));
        }

        /// <inheritdoc/>
        public override ResponseHeader SetMonitoringMode(RequestHeader? requestHeader,
            uint subscriptionId, MonitoringMode monitoringMode, UInt32Collection monitoredItemIds,
            out StatusCodeCollection results, out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(SetMonitoringMode));
        }

        /// <inheritdoc/>
        public override IAsyncResult BeginSetMonitoringMode(RequestHeader? requestHeader,
            uint subscriptionId, MonitoringMode monitoringMode, UInt32Collection monitoredItemIds,
            AsyncCallback callback, object asyncState)
        {
            throw NotSupported(nameof(BeginSetMonitoringMode));
        }

        /// <inheritdoc/>
        public override ResponseHeader EndSetMonitoringMode(IAsyncResult result,
            out StatusCodeCollection results, out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(EndSetMonitoringMode));
        }

        /// <inheritdoc/>
        public override ResponseHeader SetTriggering(RequestHeader? requestHeader,
            uint subscriptionId, uint triggeringItemId, UInt32Collection linksToAdd,
            UInt32Collection linksToRemove, out StatusCodeCollection addResults,
            out DiagnosticInfoCollection addDiagnosticInfos, out StatusCodeCollection removeResults,
            out DiagnosticInfoCollection removeDiagnosticInfos)
        {
            throw NotSupported(nameof(SetTriggering));
        }

        /// <inheritdoc/>
        public override IAsyncResult BeginSetTriggering(RequestHeader? requestHeader,
            uint subscriptionId, uint triggeringItemId, UInt32Collection linksToAdd,
            UInt32Collection linksToRemove, AsyncCallback callback, object asyncState)
        {
            throw NotSupported(nameof(BeginSetTriggering));
        }

        /// <inheritdoc/>
        public override ResponseHeader EndSetTriggering(IAsyncResult result,
            out StatusCodeCollection addResults, out DiagnosticInfoCollection addDiagnosticInfos,
            out StatusCodeCollection removeResults, out DiagnosticInfoCollection removeDiagnosticInfos)
        {
            throw NotSupported(nameof(EndSetTriggering));
        }

        /// <inheritdoc/>
        public override ResponseHeader DeleteMonitoredItems(RequestHeader? requestHeader,
            uint subscriptionId, UInt32Collection monitoredItemIds, out StatusCodeCollection results,
            out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(DeleteMonitoredItems));
        }

        /// <inheritdoc/>
        public override IAsyncResult BeginDeleteMonitoredItems(RequestHeader? requestHeader,
            uint subscriptionId, UInt32Collection monitoredItemIds, AsyncCallback callback,
            object asyncState)
        {
            throw NotSupported(nameof(BeginDeleteMonitoredItems));
        }

        /// <inheritdoc/>
        public override ResponseHeader EndDeleteMonitoredItems(IAsyncResult result, out
            StatusCodeCollection results, out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(EndDeleteMonitoredItems));
        }

        /// <inheritdoc/>
        public override ResponseHeader CreateSubscription(RequestHeader? requestHeader,
            double requestedPublishingInterval, uint requestedLifetimeCount,
            uint requestedMaxKeepAliveCount, uint maxNotificationsPerPublish,
            bool publishingEnabled, byte priority, out uint subscriptionId,
            out double revisedPublishingInterval, out uint revisedLifetimeCount,
            out uint revisedMaxKeepAliveCount)
        {
            throw NotSupported(nameof(CreateSubscription));
        }

        /// <inheritdoc/>
        public override IAsyncResult BeginCreateSubscription(RequestHeader? requestHeader,
            double requestedPublishingInterval, uint requestedLifetimeCount,
            uint requestedMaxKeepAliveCount, uint maxNotificationsPerPublish,
            bool publishingEnabled, byte priority, AsyncCallback callback, object asyncState)
        {
            throw NotSupported(nameof(BeginCreateSubscription));
        }

        /// <inheritdoc/>
        public override ResponseHeader EndCreateSubscription(IAsyncResult result,
            out uint subscriptionId, out double revisedPublishingInterval,
            out uint revisedLifetimeCount, out uint revisedMaxKeepAliveCount)
        {
            throw NotSupported(nameof(EndCreateSubscription));
        }

        /// <inheritdoc/>
        public override ResponseHeader ModifySubscription(RequestHeader? requestHeader,
            uint subscriptionId, double requestedPublishingInterval, uint requestedLifetimeCount,
            uint requestedMaxKeepAliveCount, uint maxNotificationsPerPublish, byte priority,
            out double revisedPublishingInterval, out uint revisedLifetimeCount,
            out uint revisedMaxKeepAliveCount)
        {
            throw NotSupported(nameof(ModifySubscription));
        }

        /// <inheritdoc/>
        public override IAsyncResult BeginModifySubscription(RequestHeader? requestHeader,
            uint subscriptionId, double requestedPublishingInterval, uint requestedLifetimeCount,
            uint requestedMaxKeepAliveCount, uint maxNotificationsPerPublish, byte priority,
            AsyncCallback callback, object asyncState)
        {
            throw NotSupported(nameof(BeginModifySubscription));
        }

        /// <inheritdoc/>
        public override ResponseHeader EndModifySubscription(IAsyncResult result,
            out double revisedPublishingInterval, out uint revisedLifetimeCount,
            out uint revisedMaxKeepAliveCount)
        {
            throw NotSupported(nameof(EndModifySubscription));
        }

        /// <inheritdoc/>
        public override ResponseHeader SetPublishingMode(RequestHeader? requestHeader,
            bool publishingEnabled, UInt32Collection subscriptionIds, out StatusCodeCollection results,
            out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(SetPublishingMode));
        }

        /// <inheritdoc/>
        public override IAsyncResult BeginSetPublishingMode(RequestHeader? requestHeader,
            bool publishingEnabled, UInt32Collection subscriptionIds, AsyncCallback callback,
            object asyncState)
        {
            throw NotSupported(nameof(BeginSetPublishingMode));
        }

        /// <inheritdoc/>
        public override ResponseHeader EndSetPublishingMode(IAsyncResult result,
            out StatusCodeCollection results, out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(EndSetPublishingMode));
        }

        /// <inheritdoc/>
        public override ResponseHeader Publish(RequestHeader? requestHeader,
            SubscriptionAcknowledgementCollection subscriptionAcknowledgements,
            out uint subscriptionId, out UInt32Collection availableSequenceNumbers,
            out bool moreNotifications, out NotificationMessage notificationMessage,
            out StatusCodeCollection results, out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(Publish));
        }

#if TODO
        /// <inheritdoc/>
        public override IAsyncResult BeginPublish(RequestHeader? requestHeader,
            SubscriptionAcknowledgementCollection subscriptionAcknowledgements,
            AsyncCallback callback, object asyncState)
        {
            throw NotSupported(nameof(BeginPublish));
        }

        /// <inheritdoc/>
        public override ResponseHeader EndPublish(IAsyncResult result, out uint subscriptionId,
            out UInt32Collection availableSequenceNumbers, out bool moreNotifications,
            out NotificationMessage notificationMessage, out StatusCodeCollection results,
            out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(EndPublish));
        }

        /// <inheritdoc/>
        public override IAsyncResult BeginRead(RequestHeader? requestHeader, double maxAge,
            TimestampsToReturn timestampsToReturn, ReadValueIdCollection nodesToRead,
            AsyncCallback callback, object asyncState)
        {
            throw NotSupported(nameof(BeginRead));
        }

        /// <inheritdoc/>
        public override ResponseHeader EndRead(IAsyncResult result,
            out DataValueCollection results, out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(EndRead));
        }
#endif

        /// <inheritdoc/>
        public override ResponseHeader Read(RequestHeader? requestHeader, double maxAge,
            TimestampsToReturn timestampsToReturn, ReadValueIdCollection nodesToRead,
            out DataValueCollection results, out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(Read));
        }

        /// <inheritdoc/>
        public override ResponseHeader Republish(RequestHeader? requestHeader,
            uint subscriptionId, uint retransmitSequenceNumber,
            out NotificationMessage notificationMessage)
        {
            throw NotSupported(nameof(Republish));
        }

        /// <inheritdoc/>
        public override IAsyncResult BeginRepublish(RequestHeader? requestHeader,
            uint subscriptionId, uint retransmitSequenceNumber, AsyncCallback callback,
            object asyncState)
        {
            throw NotSupported(nameof(BeginRepublish));
        }

        /// <inheritdoc/>
        public override ResponseHeader EndRepublish(IAsyncResult result,
            out NotificationMessage notificationMessage)
        {
            throw NotSupported(nameof(EndRepublish));
        }

        /// <inheritdoc/>
        public override ResponseHeader TransferSubscriptions(RequestHeader? requestHeader,
            UInt32Collection subscriptionIds, bool sendInitialValues,
            out TransferResultCollection results, out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(TransferSubscriptions));
        }

        /// <inheritdoc/>
        public override IAsyncResult BeginTransferSubscriptions(RequestHeader? requestHeader,
            UInt32Collection subscriptionIds, bool sendInitialValues, AsyncCallback callback,
            object asyncState)
        {
            throw NotSupported(nameof(BeginTransferSubscriptions));
        }

        /// <inheritdoc/>
        public override ResponseHeader EndTransferSubscriptions(IAsyncResult result,
            out TransferResultCollection results, out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(EndTransferSubscriptions));
        }

        /// <inheritdoc/>
        public override ResponseHeader DeleteSubscriptions(RequestHeader? requestHeader,
            UInt32Collection subscriptionIds, out StatusCodeCollection results,
            out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(DeleteSubscriptions));
        }

        /// <inheritdoc/>
        public override IAsyncResult BeginDeleteSubscriptions(RequestHeader? requestHeader,
            UInt32Collection subscriptionIds, AsyncCallback callback, object asyncState)
        {
            throw NotSupported(nameof(BeginDeleteSubscriptions));
        }

        /// <inheritdoc/>
        public override ResponseHeader EndDeleteSubscriptions(IAsyncResult result,
            out StatusCodeCollection results, out DiagnosticInfoCollection diagnosticInfos)
        {
            throw NotSupported(nameof(EndDeleteSubscriptions));
        }

        /// <summary>
        /// Throw not supported exception
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static ServiceResultException NotSupported(string name)
        {
            Debug.Fail(name + " not supported");
            return ServiceResultException.Create(StatusCodes.BadNotSupported,
                name + " deprecated");
        }
    }
}
