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
    using System.Runtime.Serialization;

    /// <summary>
    /// A monitored item.
    /// </summary>
    [DataContract(Namespace = Namespaces.OpcUaXsd)]
    [KnownType(typeof(DataChangeFilter))]
    [KnownType(typeof(EventFilter))]
    [KnownType(typeof(AggregateFilter))]
    public class MonitoredItem : ICloneable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MonitoredItem"/> class.
        /// </summary>
        public MonitoredItem()
        {
            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitoredItem"/> class.
        /// </summary>
        /// <param name="clientHandle">The client handle. The caller must ensure it uniquely identifies the monitored item.</param>
        public MonitoredItem(uint clientHandle)
        {
            Initialize();
            m_clientHandle = clientHandle;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitoredItem"/> class.
        /// </summary>
        /// <param name="template">The template used to specify the monitoring parameters.</param>
        public MonitoredItem(MonitoredItem template) : this(template, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitoredItem"/> class.
        /// </summary>
        /// <param name="template">The template used to specify the monitoring parameters.</param>
        /// <param name="copyEventHandlers">if set to <c>true</c> the event handlers are copied.</param>
        public MonitoredItem(MonitoredItem template, bool copyEventHandlers) : this(template, copyEventHandlers, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitoredItem"/> class.
        /// </summary>
        /// <param name="template">The template used to specify the monitoring parameters.</param>
        /// <param name="copyEventHandlers">if set to <c>true</c> the event handlers are copied.</param>
        /// <param name="copyClientHandle">if set to <c>true</c> the clientHandle is of the template copied.</param>
        public MonitoredItem(MonitoredItem template, bool copyEventHandlers, bool copyClientHandle)
        {
            Initialize();

            if (template != null)
            {
                var displayName = template.DisplayName;

                if (displayName != null)
                {
                    // remove any existing numeric suffix.
                    var index = displayName.LastIndexOf(' ');

                    if (index != -1)
                    {
                        try
                        {
                            displayName = displayName.Substring(0, index);
                        }
                        catch
                        {
                            // not a numeric suffix.
                        }
                    }
                }

                Handle = template.Handle;
                DisplayName = Utils.Format("{0} {1}", displayName, m_clientHandle);
                StartNodeId = template.StartNodeId;
                m_relativePath = template.m_relativePath;
                AttributeId = template.AttributeId;
                IndexRange = template.IndexRange;
                Encoding = template.Encoding;
                MonitoringMode = template.MonitoringMode;
                m_samplingInterval = template.m_samplingInterval;
                m_filter = (MonitoringFilter)Utils.Clone(template.m_filter);
                m_queueSize = template.m_queueSize;
                m_discardOldest = template.m_discardOldest;
                m_attributesModified = true;

                if (copyClientHandle)
                {
                    m_clientHandle = template.m_clientHandle;
                }

                // this ensures the state is consistent with the node class.
                NodeClass = template.NodeClass;
            }
        }

        /// <summary>
        /// Called by the .NET framework during deserialization.
        /// </summary>
        /// <param name="context"></param>
        [OnDeserializing]
        protected void Initialize(StreamingContext context)
        {
            Initialize();
        }

        /// <summary>
        /// Sets the private members to default values.
        /// </summary>
        private void Initialize()
        {
            StartNodeId = null;
            m_relativePath = null;
            m_clientHandle = 0;
            AttributeId = Attributes.Value;
            IndexRange = null;
            Encoding = null;
            MonitoringMode = MonitoringMode.Reporting;
            m_samplingInterval = -1;
            m_filter = null;
            m_queueSize = 0;
            m_discardOldest = true;
            m_attributesModified = true;
            m_status = new MonitoredItemStatus();

            // this ensures the state is consistent with the node class.
            NodeClass = NodeClass.Variable;

            // assign a unique handle.
            m_clientHandle = Utils.IncrementIdentifier(ref s_globalClientHandle);
        }

        /// <summary>
        /// A display name for the monitored item.
        /// </summary>
        [DataMember(Order = 1)]
        public string DisplayName { get; set; }

        /// <summary>
        /// The start node for the browse path that identifies the node to monitor.
        /// </summary>
        [DataMember(Order = 2)]
        public NodeId StartNodeId { get; set; }

        /// <summary>
        /// The node class of the node being monitored (affects the type of filter available).
        /// </summary>
        [DataMember(Order = 4)]
        public NodeClass NodeClass { get; set; }

        /// <summary>
        /// The attribute to monitor.
        /// </summary>
        [DataMember(Order = 5)]
        public uint AttributeId { get; set; }

        /// <summary>
        /// The range of array indexes to monitor.
        /// </summary>
        [DataMember(Order = 6)]
        public string IndexRange { get; set; }

        /// <summary>
        /// The encoding to use when returning notifications.
        /// </summary>
        [DataMember(Order = 7)]
        public QualifiedName Encoding { get; set; }

        /// <summary>
        /// The monitoring mode.
        /// </summary>
        [DataMember(Order = 8)]
        public MonitoringMode MonitoringMode { get; set; }

        /// <summary>
        /// The sampling interval.
        /// </summary>
        [DataMember(Order = 9)]
        public int SamplingInterval
        {
            get { return m_samplingInterval; }

            set
            {
                if (m_samplingInterval != value)
                {
                    m_attributesModified = true;
                }

                m_samplingInterval = value;
            }
        }

        /// <summary>
        /// The filter to use to select values to return.
        /// </summary>
        [DataMember(Order = 10)]
        public MonitoringFilter Filter
        {
            get { return m_filter; }

            set
            {
                // validate filter against node class.
                ValidateFilter(NodeClass, value);

                m_attributesModified = true;
                m_filter = value;
            }
        }

        /// <summary>
        /// The length of the queue used to buffer values.
        /// </summary>
        [DataMember(Order = 11)]
        public uint QueueSize
        {
            get { return m_queueSize; }

            set
            {
                if (m_queueSize != value)
                {
                    m_attributesModified = true;
                }

                m_queueSize = value;
            }
        }

        /// <summary>
        /// Whether to discard the oldest entries in the queue when it is full.
        /// </summary>
        [DataMember(Order = 12)]
        public bool DiscardOldest
        {
            get { return m_discardOldest; }

            set
            {
                if (m_discardOldest != value)
                {
                    m_attributesModified = true;
                }

                m_discardOldest = value;
            }
        }

        /// <summary>
        /// The subscription that owns the monitored item.
        /// </summary>
        public Subscription Subscription { get; internal set; }

        /// <summary>
        /// A local handle assigned to the monitored item.
        /// </summary>
        public object Handle { get; set; }

        /// <summary>
        /// Whether the item has been created on the server.
        /// </summary>
        public bool Created => m_status.Created;

        /// <summary>
        /// The identifier assigned by the client.
        /// </summary>
        public uint ClientHandle => m_clientHandle;

        /// <summary>
        /// The node id to monitor after applying any relative path.
        /// </summary>
        public NodeId ResolvedNodeId
        {
            get
            {
                // just return the start id if relative path is empty.
                if (string.IsNullOrEmpty(m_relativePath))
                {
                    return StartNodeId;
                }

                return m_resolvedNodeId;
            }

            internal set { m_resolvedNodeId = value; }
        }

        /// <summary>
        /// Whether the monitoring attributes have been modified since the item was created.
        /// </summary>
        public bool AttributesModified => m_attributesModified;

        /// <summary>
        /// The status associated with the monitored item.
        /// </summary>
        public MonitoredItemStatus Status => m_status;

        /// <inheritdoc/>
        public virtual object Clone()
        {
            return MemberwiseClone();
        }

        /// <summary>
        /// Creates a deep copy of the object.
        /// </summary>
        public new object MemberwiseClone()
        {
            return new MonitoredItem(this);
        }

        /// <summary>
        /// Clones a monitored item or the subclass with an option to copy event handlers.
        /// </summary>
        /// <param name="copyEventHandlers"></param>
        /// <param name="copyClientHandle"></param>
        /// <returns>A cloned instance of the monitored item or a subclass.</returns>
        public virtual MonitoredItem CloneMonitoredItem(bool copyEventHandlers, bool copyClientHandle)
        {
            return new MonitoredItem(this, copyEventHandlers, copyClientHandle);
        }

        /// <summary>
        /// Updates the object with the results of a create monitored item request.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="result"></param>
        /// <param name="index"></param>
        /// <param name="diagnosticInfos"></param>
        /// <param name="responseHeader"></param>
        public void SetCreateResult(
            MonitoredItemCreateRequest request,
            MonitoredItemCreateResult result,
            int index,
            DiagnosticInfoCollection diagnosticInfos,
            ResponseHeader responseHeader)
        {
            ServiceResult error = null;

            if (StatusCode.IsBad(result.StatusCode))
            {
                error = ClientBase.GetResult(result.StatusCode, index, diagnosticInfos, responseHeader);
            }

            m_status.SetCreateResult(request, result, error);
            m_attributesModified = false;
        }

        /// <summary>
        /// Updates the object with the results of a modify monitored item request.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="result"></param>
        /// <param name="index"></param>
        /// <param name="diagnosticInfos"></param>
        /// <param name="responseHeader"></param>
        public void SetModifyResult(
            MonitoredItemModifyRequest request,
            MonitoredItemModifyResult result,
            int index,
            DiagnosticInfoCollection diagnosticInfos,
            ResponseHeader responseHeader)
        {
            ServiceResult error = null;

            if (StatusCode.IsBad(result.StatusCode))
            {
                error = ClientBase.GetResult(result.StatusCode, index, diagnosticInfos, responseHeader);
            }

            m_status.SetModifyResult(request, result, error);
            m_attributesModified = false;
        }

        /// <summary>
        /// Updates the object with the results of a transfer subscription request.
        /// </summary>
        /// <param name="clientHandle"></param>
        public void SetTransferResult(uint clientHandle)
        {
            // ensure the global counter is not duplicating future handle ids
            Utils.LowerLimitIdentifier(ref s_globalClientHandle, clientHandle);
            m_clientHandle = clientHandle;
            m_status.SetTransferResult(this);
            m_attributesModified = false;
        }

        /// <summary>
        /// Updates the object with the results of a delete monitored item request.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="index"></param>
        /// <param name="diagnosticInfos"></param>
        /// <param name="responseHeader"></param>
        public void SetDeleteResult(
            StatusCode result,
            int index,
            DiagnosticInfoCollection diagnosticInfos,
            ResponseHeader responseHeader)
        {
            ServiceResult error = null;

            if (StatusCode.IsBad(result))
            {
                error = ClientBase.GetResult(result, index, diagnosticInfos, responseHeader);
            }

            m_status.SetDeleteResult(error);
        }

        /// <summary>
        /// Throws an exception if the flter cannot be used with the node class.
        /// </summary>
        /// <param name="nodeClass"></param>
        /// <param name="filter"></param>
        /// <exception cref="ServiceResultException"></exception>
        private void ValidateFilter(NodeClass nodeClass, MonitoringFilter filter)
        {
            if (filter == null)
            {
                return;
            }

            switch (nodeClass)
            {
                case NodeClass.Variable:
                case NodeClass.VariableType:
                    {
                        if (!typeof(DataChangeFilter).IsInstanceOfType(filter))
                        {
                            NodeClass = NodeClass.Variable;
                        }

                        break;
                    }

                case NodeClass.Object:
                case NodeClass.View:
                    {
                        if (!typeof(EventFilter).IsInstanceOfType(filter))
                        {
                            NodeClass = NodeClass.Object;
                        }

                        break;
                    }

                default:
                    {
                        throw ServiceResultException.Create(StatusCodes.BadFilterNotAllowed, "Filters may not be specified for nodes of class '{0}'.", nodeClass);
                    }
            }
        }

        /// <summary>
        /// Sets the default event filter.
        /// </summary>
        private void UseDefaultEventFilter()
        {
            EventFilter filter = filter = new EventFilter();

            filter.AddSelectClause(ObjectTypes.BaseEventType, Opc.Ua.BrowseNames.EventId);
            filter.AddSelectClause(ObjectTypes.BaseEventType, Opc.Ua.BrowseNames.EventType);
            filter.AddSelectClause(ObjectTypes.BaseEventType, Opc.Ua.BrowseNames.SourceNode);
            filter.AddSelectClause(ObjectTypes.BaseEventType, Opc.Ua.BrowseNames.SourceName);
            filter.AddSelectClause(ObjectTypes.BaseEventType, Opc.Ua.BrowseNames.Time);
            filter.AddSelectClause(ObjectTypes.BaseEventType, Opc.Ua.BrowseNames.ReceiveTime);
            filter.AddSelectClause(ObjectTypes.BaseEventType, Opc.Ua.BrowseNames.LocalTime);
            filter.AddSelectClause(ObjectTypes.BaseEventType, Opc.Ua.BrowseNames.Message);
            filter.AddSelectClause(ObjectTypes.BaseEventType, Opc.Ua.BrowseNames.Severity);

            m_filter = filter;
        }

        private string m_relativePath;
        private NodeId m_resolvedNodeId;
        private int m_samplingInterval;
        private MonitoringFilter m_filter;
        private uint m_queueSize;
        private bool m_discardOldest;
        private uint m_clientHandle;
        private MonitoredItemStatus m_status;
        private bool m_attributesModified;
        private static long s_globalClientHandle;
    }
}
