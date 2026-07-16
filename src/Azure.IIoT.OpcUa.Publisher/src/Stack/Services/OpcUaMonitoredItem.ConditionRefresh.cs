// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using Opc.Ua.Client;
    using System;
    using System.Diagnostics;
    using System.Runtime.Serialization;

    internal abstract partial class OpcUaMonitoredItem
    {
        /// <summary>
        /// A monitored event item that participates in the subscription wide
        /// one-time ConditionRefresh (issued when the item first becomes good
        /// and again after a reconnect) so retained conditions already present
        /// on the server are delivered once. Unlike <see cref="Condition"/> it
        /// does not cache conditions or periodically re-send snapshots.
        /// </summary>
        internal interface IConditionRefreshable
        {
            /// <summary>
            /// Whether a subscription wide condition refresh is required for
            /// this item (it (re-)entered a good state since the last refresh).
            /// </summary>
            bool IsConditionRefreshRequired { get; }

            /// <summary>
            /// Mark that a subscription wide condition refresh has completed for
            /// this item while it was in a good state.
            /// </summary>
            void OnConditionRefreshCompleted();
        }

        /// <summary>
        /// Refresh-only condition item. Behaves like a regular event
        /// subscription (events are forwarded as they arrive) but requests a
        /// single ConditionRefresh once established so retained conditions are
        /// surfaced once. The RefreshStart/RefreshEnd envelope markers are
        /// suppressed; a RefreshRequired notification re-issues the refresh.
        /// </summary>
        [DataContract(Namespace = Namespaces.OpcUaXsd)]
        [KnownType(typeof(DataChangeFilter))]
        [KnownType(typeof(EventFilter))]
        [KnownType(typeof(AggregateFilter))]
        internal sealed class ConditionRefresh : Event, IConditionRefreshable
        {
            /// <inheritdoc/>
            public bool IsConditionRefreshRequired => ConditionRefreshRequired;

            /// <inheritdoc/>
            public void OnConditionRefreshCompleted() => MarkConditionRefreshCompleted();

            /// <summary>
            /// Create refresh-only condition item
            /// </summary>
            /// <param name="owner"></param>
            /// <param name="template"></param>
            /// <param name="logger"></param>
            /// <param name="timeProvider"></param>
            public ConditionRefresh(ISubscriber owner, EventMonitoredItemModel template,
                ILogger<Event> logger, TimeProvider timeProvider) :
                base(owner, template, logger, timeProvider)
            {
            }

            /// <summary>
            /// Copy constructor
            /// </summary>
            /// <param name="item"></param>
            /// <param name="copyEventHandlers"></param>
            /// <param name="copyClientHandle"></param>
            private ConditionRefresh(ConditionRefresh item, bool copyEventHandlers,
                bool copyClientHandle)
                : base(item, copyEventHandlers, copyClientHandle)
            {
                // The refresh-sent flag lives in the base Event and is
                // intentionally not copied so a cloned item re-issues a refresh
                // once it is good again to re-establish state.
            }

            /// <inheritdoc/>
            public override MonitoredItem CloneMonitoredItem(
                bool copyEventHandlers, bool copyClientHandle)
            {
                return new ConditionRefresh(this, copyEventHandlers, copyClientHandle);
            }

            /// <inheritdoc/>
            public override bool Equals(object? obj)
            {
                if (obj is not ConditionRefresh item)
                {
                    return false;
                }
                return base.Equals(item);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                return 2035794739 + base.GetHashCode();
            }

            /// <inheritdoc/>
            public override string ToString()
            {
                var str = $"Condition Refresh Item '{Template.StartNodeId}'";
                if (RemoteId.HasValue)
                {
                    str += $" with server id {RemoteId} " +
                        $"({(Status?.Created == true ? "" : "not ")}created)";
                }
                return str;
            }

            /// <inheritdoc/>
            protected override bool ProcessEventNotification(DateTimeOffset timestamp,
                EventFieldList eventFields, MonitoredItemNotifications notifications)
            {
                Debug.Assert(Valid);
                Debug.Assert(Template != null);

                var eventType = GetRefreshEventType(eventFields);
                if (eventType == ObjectTypeIds.RefreshStartEventType ||
                    eventType == ObjectTypeIds.RefreshEndEventType)
                {
                    // Suppress the refresh envelope markers - only the retained
                    // condition events emitted between them are forwarded.
                    return true;
                }
                if (eventType == ObjectTypeIds.RefreshRequiredEventType)
                {
                    // Re-issue a condition refresh to re-establish correct state.
                    IssueConditionRefresh();
                    return true;
                }

                // Forward everything else as a regular event notification.
                return base.ProcessEventNotification(timestamp, eventFields, notifications);
            }
        }
    }
}
