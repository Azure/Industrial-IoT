// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

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
    public abstract class MonitoredItem : IDisposable
    {
        /// <summary>
        /// A display name for the monitored item.
        /// </summary>
        [DataMember(Order = 1)]
        public string? DisplayName { get; set; }

        /// <summary>
        /// The start node for the browse path that
        /// identifies the node to monitor.
        /// </summary>
        [DataMember(Order = 2)]
        public NodeId StartNodeId { get; set; }

        /// <summary>
        /// The node class of the node being monitored
        /// (affects the type of filter available).
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
        public TimeSpan SamplingInterval
        {
            get => _samplingInterval;
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
            get => _filter;
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
            get => _queueSize;
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
            get => _discardOldest;
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
        /// Whether the monitoring attributes have been modified
        /// since the item was created.
        /// </summary>
        public bool AttributesModified { get; private set; }

        /// <summary>
        /// The status associated with the monitored item.
        /// </summary>
        public MonitoredItemStatus Status { get; }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        protected MonitoredItem()
        {
            StartNodeId = NodeId.Null;
            AttributeId = Attributes.Value;
            MonitoringMode = MonitoringMode.Reporting;
            AttributesModified = true;
            Status = new MonitoredItemStatus();
            ClientHandle = Utils.IncrementIdentifier(ref _globalClientHandle);
            _samplingInterval = TimeSpan.MinValue;
            _discardOldest = true;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose monitored item
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            _disposedValue = true;
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
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            var error = ServiceResult.Good;

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
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            var error = ServiceResult.Good;

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
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            // ensure the global counter is not duplicating future handle ids
            Utils.LowerLimitIdentifier(ref _globalClientHandle, clientHandle);
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
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            var error = ServiceResult.Good;
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
                case AggregateFilter:
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
                case DataChangeFilter:
                    if (NodeClass == NodeClass.Unspecified)
                    {
                        NodeClass = NodeClass.Variable;
                    }
                    else if (NodeClass is not NodeClass.Variable and not NodeClass.VariableType)
                    {
                        throw ServiceResultException.Create(StatusCodes.BadFilterNotAllowed,
                            "DataChangeFilter may not be specified for nodes of class '{0}'.",
                            NodeClass);
                    }
                    break;
                case EventFilter:
                    if (NodeClass == NodeClass.Unspecified)
                    {
                        NodeClass = NodeClass.Object;
                    }
                    else if (NodeClass is not NodeClass.Object and not NodeClass.View)
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

        private TimeSpan _samplingInterval;
        private MonitoringFilter? _filter;
        private uint _queueSize;
        private bool _discardOldest;
        private static long _globalClientHandle;
        private bool _disposedValue;
    }
}
