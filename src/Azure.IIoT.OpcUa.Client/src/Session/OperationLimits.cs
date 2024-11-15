// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Extended operation limits
    /// </summary>
    [DataContract(Namespace = Namespaces.OpcUaConfig)]
    public sealed class OperationLimits : Opc.Ua.OperationLimits
    {
        /// <summary>
        /// Max browse continuation points
        /// </summary>
        [DataMember(Order = 200)]
        public uint MaxBrowseContinuationPoints { get; set; }

        /// <summary>
        /// Max query continuation points
        /// </summary>
        [DataMember(Order = 210)]
        public uint MaxQueryContinuationPoints { get; set; }

        /// <summary>
        /// Max history continuation points
        /// </summary>
        [DataMember(Order = 220)]
        public uint MaxHistoryContinuationPoints { get; set; }

        /// <summary>
        /// Max nodes that can be part of a browse path
        /// </summary>
        [DataMember(Order = 230)]
        public uint MaxNodesPerTranslatePathsToNodeIds { get; set; }

        /// <summary>
        /// Min supported sampling rate
        /// </summary>
        [DataMember(Order = 240)]
        public double? MinSupportedSampleRate { get; set; }

        /// <summary>
        /// Max array length supported
        /// </summary>
        [DataMember(Order = 250)]
        public uint MaxArrayLength { get; set; }

        /// <summary>
        /// Max string length supported
        /// </summary>
        [DataMember(Order = 260)]
        public uint MaxStringLength { get; set; }

        /// <summary>
        /// Max byte buffer length supported
        /// </summary>
        [DataMember(Order = 270)]
        public uint MaxByteStringLength { get; set; }
    }
}
