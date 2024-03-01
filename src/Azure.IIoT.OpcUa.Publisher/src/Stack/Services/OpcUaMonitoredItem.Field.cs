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
    using Opc.Ua.Client.ComplexTypes;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Threading.Tasks;

    internal abstract partial class OpcUaMonitoredItem
    {
        /// <summary>
        /// Extension Field item
        /// </summary>
        [DataContract(Namespace = Namespaces.OpcUaXsd)]
        [KnownType(typeof(DataChangeFilter))]
        [KnownType(typeof(EventFilter))]
        [KnownType(typeof(AggregateFilter))]
        internal class Field : OpcUaMonitoredItem
        {
            /// <summary>
            /// Item as extension field
            /// </summary>
            public ExtensionFieldItemModel Template { get; protected internal set; }

            /// <summary>
            /// Create wrapper
            /// </summary>
            /// <param name="template"></param>
            /// <param name="logger"></param>
            public Field(ExtensionFieldItemModel template,
                ILogger<Field> logger) : base(logger, template.StartNodeId)
            {
                Template = template;
            }

            /// <summary>
            /// Copy constructor
            /// </summary>
            /// <param name="item"></param>
            /// <param name="copyEventHandlers"></param>
            /// <param name="copyClientHandle"></param>
            protected Field(Field item, bool copyEventHandlers,
                bool copyClientHandle)
                : base(item, copyEventHandlers, copyClientHandle)
            {
                Template = item.Template;
                _fieldId = item._fieldId;
                _value = item._value;
            }

            /// <inheritdoc/>
            public override MonitoredItem CloneMonitoredItem(
                bool copyEventHandlers, bool copyClientHandle)
            {
                return new Field(this, copyEventHandlers, copyClientHandle);
            }

            /// <inheritdoc/>
            public override bool Equals(object? obj)
            {
                if (obj is not Field fieldItem)
                {
                    return false;
                }
                if ((Template.DataSetFieldName ?? string.Empty) !=
                    (fieldItem.Template.DataSetFieldName ?? string.Empty))
                {
                    return false;
                }
                if (Template.Value != fieldItem.Template.Value)
                {
                    return false;
                }
                return true;
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                var hashCode = 81523234;
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<string>.Default.GetHashCode(
                        Template.DataSetFieldName ?? string.Empty);
                hashCode = (hashCode * -1521134295) +
                    Template.Value.GetHashCode();
                return hashCode;
            }

            /// <inheritdoc/>
            public override string ToString()
            {
                return $"Field '{Template.DataSetFieldName}' with value {Template.Value}.";
            }

            /// <inheritdoc/>
            public override bool AddTo(Subscription subscription,
                IOpcUaSession session)
            {
                _value = new DataValue(session.Codec.Decode(Template.Value, BuiltInType.Variant));
                Valid = true;
                return true;
            }

            /// <inheritdoc/>
            public override bool MergeWith(OpcUaMonitoredItem item, IOpcUaSession session)
            {
                return false;
            }

            /// <inheritdoc/>
            public override bool RemoveFrom(Subscription subscription)
            {
                _value = new DataValue();
                Valid = false;
                return true;
            }

            /// <inheritdoc/>
            public override bool TryCompleteChanges(Subscription subscription,
                ref bool applyChanges,
                Callback cb)
            {
                return true;
            }

            /// <inheritdoc/>
            public override bool TryGetLastMonitoredItemNotifications(uint sequenceNumber,
                IList<MonitoredItemNotificationModel> notifications)
            {
                if (!Valid)
                {
                    return false;
                }
                notifications.Add(ToMonitoredItemNotification(sequenceNumber));
                return true;
            }

            /// <inheritdoc/>
            public override bool TryGetMonitoredItemNotifications(uint sequenceNumber,
                DateTime timestamp, IEncodeable evt, IList<MonitoredItemNotificationModel> notifications)
            {
                Debug.Fail("Unexpected notification on extension field");
                return false;
            }

            /// <inheritdoc/>
            protected override IEnumerable<OpcUaMonitoredItem> CreateTriggeredItems(
                ILoggerFactory factory, IOpcUaClient? client = null)
            {
                return Enumerable.Empty<OpcUaMonitoredItem>();
            }

            /// <inheritdoc/>
            protected override bool TryGetErrorMonitoredItemNotifications(
                uint sequenceNumber, StatusCode statusCode,
                IList<MonitoredItemNotificationModel> notifications)
            {
                Debug.Fail("Unexpected notification on extension field");
                return false;
            }

            /// <summary>
            /// Convert to monitored item notifications
            /// </summary>
            /// <param name="sequenceNumber"></param>
            /// <returns></returns>
            protected MonitoredItemNotificationModel ToMonitoredItemNotification(uint sequenceNumber)
            {
                Debug.Assert(Valid);
                Debug.Assert(Template != null);

                return new MonitoredItemNotificationModel
                {
                    Id = Template.Id,
                    DataSetFieldName = Template.DisplayName,
                    Context = Template.Context,
                    DataSetName = Template.DisplayName,
                    NodeId = NodeId,
                    Value = _value,
                    Flags = 0,
                    SequenceNumber = sequenceNumber
                };
            }

            private DataValue _value = new();
            private readonly Guid _fieldId = Guid.NewGuid();
        }
    }
}
