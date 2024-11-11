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
        /// A display name for the monitored item.
        /// </summary>
        [DataMember(Order = 1)]
        public string? DisplayName { get; set; }

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
        public string? IndexRange { get; set; }

        /// <summary>
        /// The encoding to use when returning notifications.
        /// </summary>
        [DataMember(Order = 7)]
        public QualifiedName? Encoding { get; set; }

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
            get { return _samplingInterval; }

            set
            {
                if (_samplingInterval != value)
                {
                    AttributesModified = true;
                }

                _samplingInterval = value;
            }
        }

        /// <summary>
        /// The filter to use to select values to return.
        /// </summary>
        [DataMember(Order = 10)]
        public MonitoringFilter? Filter
        {
            get { return _filter; }
            set
            {
                // validate filter against node class.
                ValidateFilter(value);

                AttributesModified = true;
                _filter = value;
            }
        }

        /// <summary>
        /// The length of the queue used to buffer values.
        /// </summary>
        [DataMember(Order = 11)]
        public uint QueueSize
        {
            get { return _queueSize; }

            set
            {
                if (_queueSize != value)
                {
                    AttributesModified = true;
                }

                _queueSize = value;
            }
        }

        /// <summary>
        /// Whether to discard the oldest entries in the queue when it is full.
        /// </summary>
        [DataMember(Order = 12)]
        public bool DiscardOldest
        {
            get { return _discardOldest; }

            set
            {
                if (_discardOldest != value)
                {
                    AttributesModified = true;
                }

                _discardOldest = value;
            }
        }

        /// <summary>
        /// The subscription that owns the monitored item.
        /// </summary>
        public Subscription? Subscription { get; internal set; }

        /// <summary>
        /// A local handle assigned to the monitored item.
        /// </summary>
        public object? Handle { get; set; }

        /// <summary>
        /// Whether the item has been created on the server.
        /// </summary>
        public bool Created => Status.Created;

        /// <summary>
        /// The identifier assigned by the client.
        /// </summary>
        public uint ClientHandle { get; private set; }

        /// <summary>
        /// The node id to monitor after applying any relative path.
        /// </summary>
        public NodeId? ResolvedNodeId => StartNodeId;

        /// <summary>
        /// Whether the monitoring attributes have been modified since the item was created.
        /// </summary>
        public bool AttributesModified { get; private set; }

        /// <summary>
        /// The status associated with the monitored item.
        /// </summary>
        public MonitoredItemStatus Status { get; }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public MonitoredItem()
        {
            StartNodeId = NodeId.Null;
            AttributeId = Attributes.Value;
            MonitoringMode = MonitoringMode.Reporting;
            _samplingInterval = -1;
            _discardOldest = true;
            AttributesModified = true;
            Status = new MonitoredItemStatus();
            ClientHandle = Utils.IncrementIdentifier(ref s_globalClientHandle);
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="template">The template used to specify the
        /// monitoring parameters.</param>
        /// <param name="copyEventHandlers">if set to <c>true</c>
        /// the event handlers are copied.</param>
        /// <param name="copyClientHandle">if set to <c>true</c> the
        /// clientHandle is of the template copied.</param>
        public MonitoredItem(MonitoredItem template,
            bool copyEventHandlers, bool copyClientHandle)
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
                        displayName = displayName[..index];
                    }
                    catch
                    {
                        // not a numeric suffix.
                    }
                }
            }

            Status = new MonitoredItemStatus();
            ClientHandle = copyClientHandle ? template.ClientHandle :
                Utils.IncrementIdentifier(ref s_globalClientHandle);

            Handle = template.Handle;
            DisplayName = Utils.Format("{0} {1}", displayName, ClientHandle);
            StartNodeId = template.StartNodeId;
            AttributeId = template.AttributeId;
            IndexRange = template.IndexRange;
            Encoding = template.Encoding;
            MonitoringMode = template.MonitoringMode;

            // this ensures the state is consistent with the node class.
            NodeClass = template.NodeClass;

            _samplingInterval = template._samplingInterval;
            _filter = (MonitoringFilter)Utils.Clone(template._filter);
            _queueSize = template._queueSize;
            _discardOldest = template._discardOldest;
            AttributesModified = true;
        }

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
            return new MonitoredItem(this, false, false);
        }

        /// <summary>
        /// Clones a monitored item or the subclass with an option to copy
        /// event handlers.
        /// </summary>
        /// <param name="copyEventHandlers"></param>
        /// <param name="copyClientHandle"></param>
        /// <returns>A cloned instance of the monitored item.</returns>
        public virtual MonitoredItem CloneMonitoredItem(bool copyEventHandlers,
            bool copyClientHandle)
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
        public void SetCreateResult(MonitoredItemCreateRequest request,
            MonitoredItemCreateResult result, int index,
            DiagnosticInfoCollection diagnosticInfos, ResponseHeader responseHeader)
        {
            ServiceResult error = ServiceResult.Good;

            if (StatusCode.IsBad(result.StatusCode))
            {
                error = ClientBase.GetResult(result.StatusCode, index,
                    diagnosticInfos, responseHeader);
            }

            Status.SetCreateResult(request, result, error);
            AttributesModified = false;
        }

        /// <summary>
        /// Updates the object with the results of a modify monitored item request.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="result"></param>
        /// <param name="index"></param>
        /// <param name="diagnosticInfos"></param>
        /// <param name="responseHeader"></param>
        public void SetModifyResult(MonitoredItemModifyRequest request,
            MonitoredItemModifyResult result, int index,
            DiagnosticInfoCollection diagnosticInfos, ResponseHeader responseHeader)
        {
            ServiceResult error = ServiceResult.Good;

            if (StatusCode.IsBad(result.StatusCode))
            {
                error = ClientBase.GetResult(result.StatusCode, index,
                    diagnosticInfos, responseHeader);
            }

            Status.SetModifyResult(request, result, error);
            AttributesModified = false;
        }

        /// <summary>
        /// Updates the object with the results of a transfer subscription request.
        /// </summary>
        /// <param name="clientHandle"></param>
        public void SetTransferResult(uint clientHandle)
        {
            // ensure the global counter is not duplicating future handle ids
            Utils.LowerLimitIdentifier(ref s_globalClientHandle, clientHandle);
            ClientHandle = clientHandle;
            Status.SetTransferResult(this);
            AttributesModified = false;
        }

        /// <summary>
        /// Updates the object with the results of a delete monitored item request.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="index"></param>
        /// <param name="diagnosticInfos"></param>
        /// <param name="responseHeader"></param>
        public void SetDeleteResult(StatusCode result, int index,
            DiagnosticInfoCollection diagnosticInfos, ResponseHeader responseHeader)
        {
            ServiceResult error = ServiceResult.Good;

            if (StatusCode.IsBad(result))
            {
                error = ClientBase.GetResult(result, index, diagnosticInfos, responseHeader);
            }

            Status.SetDeleteResult(error);
        }

        /// <summary>
        /// Throws an exception if the flter cannot be used with the node class.
        /// </summary>
        /// <param name="filter"></param>
        /// <exception cref="ServiceResultException"></exception>
        private void ValidateFilter(MonitoringFilter? filter)
        {
            if (filter == null)
            {
                return;
            }
            switch (filter)
            {
                case AggregateFilter af:
                    if (NodeClass == NodeClass.Unspecified)
                    {
                        NodeClass = NodeClass.Variable;
                    }
                    else if (NodeClass != NodeClass.Variable)
                    {
                        throw ServiceResultException.Create(StatusCodes.BadFilterNotAllowed,
                            "AggregateFilter may not be specified for nodes of class '{0}'.",
                            NodeClass);
                    }
                    break;
                case DataChangeFilter dfe:
                    if (NodeClass == NodeClass.Unspecified)
                    {
                        NodeClass = NodeClass.Variable;
                    }
                    else if (NodeClass != NodeClass.Variable && NodeClass != NodeClass.VariableType)
                    {
                        throw ServiceResultException.Create(StatusCodes.BadFilterNotAllowed,
                            "DataChangeFilter may not be specified for nodes of class '{0}'.",
                            NodeClass);
                    }
                    break;
                case EventFilter ef:
                    if (NodeClass == NodeClass.Unspecified)
                    {
                        NodeClass = NodeClass.Object;
                    }
                    else if (NodeClass != NodeClass.Object && NodeClass != NodeClass.View)
                    {
                        throw ServiceResultException.Create(StatusCodes.BadFilterNotAllowed,
                            "EventFilter may not be specified for nodes of class '{0}'.",
                            NodeClass);
                    }
                    break;
                case null:
                    break;
                default:
                    throw ServiceResultException.Create(StatusCodes.BadFilterNotAllowed,
                        "Unsupported filter '{0}'.",
                        filter.GetType().Name);
            }
        }

        private int _samplingInterval;
        private MonitoringFilter? _filter;
        private uint _queueSize;
        private bool _discardOldest;
        private static long s_globalClientHandle;
    }
}
