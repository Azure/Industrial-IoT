// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Server limits
    /// </summary>
    [DataContract]
    public sealed record class OperationLimitsModel
    {
        /// <summary>
        /// Min supported sampling rate
        /// </summary>
        [DataMember(Name = "minSupportedSampleRate", Order = 0,
            EmitDefaultValue = false)]
        public double? MinSupportedSampleRate { get; set; }

        /// <summary>
        /// Max browse continuation points
        /// </summary>
        [DataMember(Name = "maxBrowseContinuationPoints", Order = 1,
            EmitDefaultValue = false)]
        public ushort? MaxBrowseContinuationPoints { get; set; }

        /// <summary>
        /// Max query continuation points
        /// </summary>
        [DataMember(Name = "maxQueryContinuationPoints", Order = 2,
            EmitDefaultValue = false)]
        public ushort? MaxQueryContinuationPoints { get; set; }

        /// <summary>
        /// Max history continuation points
        /// </summary>
        [DataMember(Name = "maxHistoryContinuationPoints", Order = 3,
            EmitDefaultValue = false)]
        public ushort? MaxHistoryContinuationPoints { get; set; }

        /// <summary>
        /// Max array length supported
        /// </summary>
        [DataMember(Name = "maxArrayLength", Order = 4,
            EmitDefaultValue = false)]
        public uint? MaxArrayLength { get; set; }

        /// <summary>
        /// Max string length supported
        /// </summary>
        [DataMember(Name = "maxStringLength", Order = 5,
            EmitDefaultValue = false)]
        public uint? MaxStringLength { get; set; }

        /// <summary>
        /// Max byte buffer length supported
        /// </summary>
        [DataMember(Name = "maxByteStringLength", Order = 6,
            EmitDefaultValue = false)]
        public uint? MaxByteStringLength { get; set; }

        /// <summary>
        /// Max nodes that can be part of a single browse call.
        /// </summary>
        [DataMember(Name = "maxNodesPerBrowse", Order = 7,
            EmitDefaultValue = false)]
        public uint? MaxNodesPerBrowse { get; set; }

        /// <summary>
        /// Max nodes that can be read in single read call
        /// </summary>
        [DataMember(Name = "maxNodesPerRead", Order = 8,
            EmitDefaultValue = false)]
        public uint? MaxNodesPerRead { get; set; }

        /// <summary>
        /// Max nodes that can be read in single write call
        /// </summary>
        [DataMember(Name = "maxNodesPerWrite", Order = 9,
            EmitDefaultValue = false)]
        public uint? MaxNodesPerWrite { get; set; }

        /// <summary>
        /// Max nodes that can be read in single method call
        /// </summary>
        [DataMember(Name = "maxNodesPerMethodCall", Order = 10,
            EmitDefaultValue = false)]
        public uint? MaxNodesPerMethodCall { get; set; }

        /// <summary>
        /// Number of nodes that can be in a History Read value call
        /// </summary>
        [DataMember(Name = "maxNodesPerHistoryReadData", Order = 11,
            EmitDefaultValue = false)]
        public uint? MaxNodesPerHistoryReadData { get; set; }

        /// <summary>
        /// Number of nodes that can be in a History Read events call
        /// </summary>
        [DataMember(Name = "maxNodesPerHistoryReadEvents", Order = 12,
            EmitDefaultValue = false)]
        public uint? MaxNodesPerHistoryReadEvents { get; set; }

        /// <summary>
        /// Number of nodes that can be in a History Update call
        /// </summary>
        [DataMember(Name = "maxNodesPerHistoryUpdateData", Order = 13,
            EmitDefaultValue = false)]
        public uint? MaxNodesPerHistoryUpdateData { get; set; }

        /// <summary>
        /// Number of nodes that can be in a History events update call
        /// </summary>
        [DataMember(Name = "maxNodesPerHistoryUpdateEvents", Order = 14,
            EmitDefaultValue = false)]
        public uint? MaxNodesPerHistoryUpdateEvents { get; set; }

        /// <summary>
        /// Max nodes that can be registered at once
        /// </summary>
        [DataMember(Name = "maxNodesPerRegisterNodes", Order = 15,
            EmitDefaultValue = false)]
        public uint? MaxNodesPerRegisterNodes { get; set; }

        /// <summary>
        /// Max nodes that can be part of a browse path
        /// </summary>
        [DataMember(Name = "maxNodesPerTranslatePathsToNodeIds", Order = 16,
            EmitDefaultValue = false)]
        public uint? MaxNodesPerTranslatePathsToNodeIds { get; set; }

        /// <summary>
        /// Max nodes that can be added or removed in a single call.
        /// </summary>
        [DataMember(Name = "maxNodesPerNodeManagement", Order = 17,
            EmitDefaultValue = false)]
        public uint? MaxNodesPerNodeManagement { get; set; }

        /// <summary>
        /// Max monitored items that can be updated at once.
        /// </summary>
        [DataMember(Name = "maxMonitoredItemsPerCall", Order = 18,
            EmitDefaultValue = false)]
        public uint? MaxMonitoredItemsPerCall { get; set; }
    }
}
