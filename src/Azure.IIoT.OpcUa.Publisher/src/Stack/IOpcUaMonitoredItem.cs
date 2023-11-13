// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Encoders.PubSub;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Client.ComplexTypes;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Update display name
    /// </summary>
    /// <param name="displayName"></param>
    public delegate void UpdateString(string displayName);

    /// <summary>
    /// Update node id
    /// </summary>
    /// <param name="nodeId"></param>
    /// <param name="messageContext"></param>
    public delegate void UpdateNodeId(NodeId nodeId,
        IServiceMessageContext messageContext);

    /// <summary>
    /// Monitored item handle
    /// </summary>
    public interface IOpcUaMonitoredItem : IDisposable
    {
        /// <summary>
        /// Monitored item once added to the subscription. Contract:
        /// The item will be null until the subscription calls
        /// <see cref="AddTo(Subscription, IOpcUaSession, out bool)"/>
        /// to add it to the subscription. After removal the item
        /// is still valid, but the Handle is null. The item is
        /// again null after <see cref="IDisposable.Dispose"/> is
        /// called.
        /// </summary>
        MonitoredItem? Item { get; }

        /// <summary>
        /// Data set name
        /// </summary>
        string? DataSetName { get; }

        /// <summary>
        /// Resolve relative path first. If this returns null
        /// the relative path either does not exist or we let
        /// subscription take care of resolving the path.
        /// </summary>
        (string NodeId, string[] Path, UpdateNodeId Update)? Resolve { get; }

        /// <summary>
        /// Register node updater. If this property is null then
        /// the node does not need to be registered.
        /// </summary>
        (string NodeId, UpdateNodeId Update)? Register { get; }

        /// <summary>
        /// Get the display name for the node. This is called after
        /// the node is resolved and registered as applicable.
        /// </summary>
        (string NodeId, UpdateString Update)? DisplayName { get; }

        /// <summary>
        /// Add the item to the subscription
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="session"></param>
        /// <param name="metadata"></param>
        /// <returns></returns>
        bool AddTo(Subscription subscription,
            IOpcUaSession session, out bool metadata);

        /// <summary>
        /// Merge item in the subscription with this item
        /// </summary>
        /// <param name="item"></param>
        /// <param name="session"></param>
        /// <param name="metadataChange"></param>
        /// <returns></returns>
        bool MergeWith(IOpcUaMonitoredItem item, IOpcUaSession session,
            out bool metadataChange);

        /// <summary>
        /// Remove from subscription
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="metadata"></param>
        /// <returns></returns>
        bool RemoveFrom(Subscription subscription, out bool metadata);

        /// <summary>
        /// Complete changes previously made and provide callback
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="applyChanges"></param>
        /// <param name="cb"></param>
        /// <returns></returns>
        bool TryCompleteChanges(Subscription subscription,
            ref bool applyChanges, Action<MessageType, string?,
                IEnumerable<MonitoredItemNotificationModel>> cb);

        /// <summary>
        /// Get any changes in the monitoring mode to apply if any.
        /// Otherwise the returned value is null.
        /// </summary>
        MonitoringMode? GetMonitoringModeChange();

        /// <summary>
        /// Try and get metadata for the item
        /// </summary>
        /// <param name="session"></param>
        /// <param name="typeSystem"></param>
        /// <param name="fields"></param>
        /// <param name="dataTypes"></param>
        /// <param name="ct"></param>
        ValueTask GetMetaDataAsync(IOpcUaSession session,
            ComplexTypeSystem? typeSystem, FieldMetaDataCollection fields,
            NodeIdDictionary<DataTypeDescription> dataTypes,
            CancellationToken ct);

        /// <summary>
        /// Subscription state changed
        /// </summary>
        /// <param name="online"></param>
        void OnMonitoredItemStateChanged(bool online);

        /// <summary>
        /// Try get monitored item notifications from
        /// the subscription's monitored item event payload.
        /// </summary>
        /// <param name="sequenceNumber"></param>
        /// <param name="timestamp"></param>
        /// <param name="encodeablePayload"></param>
        /// <param name="notifications"></param>
        /// <returns></returns>
        bool TryGetMonitoredItemNotifications(uint sequenceNumber,
            DateTime timestamp, IEncodeable encodeablePayload,
            IList<MonitoredItemNotificationModel> notifications);

        /// <summary>
        /// Get last monitored item notification saved
        /// </summary>
        /// <param name="sequenceNumber"></param>
        /// <param name="notifications"></param>
        /// <returns></returns>
        bool TryGetLastMonitoredItemNotifications(uint sequenceNumber,
            IList<MonitoredItemNotificationModel> notifications);
    }
}
