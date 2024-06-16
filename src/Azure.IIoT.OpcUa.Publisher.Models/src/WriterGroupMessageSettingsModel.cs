// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Writer group message configuration
    /// </summary>
    [DataContract]
    public sealed record class WriterGroupMessageSettingsModel
    {
        /// <summary>
        /// Network message content
        /// </summary>
        [DataMember(Name = "networkMessageContentMask", Order = 0,
            EmitDefaultValue = false)]
        public NetworkMessageContentFlags? NetworkMessageContentMask { get; set; }

        /// <summary>
        /// Group version
        /// </summary>
        [DataMember(Name = "groupVersion", Order = 1,
            EmitDefaultValue = false)]
        public uint? GroupVersion { get; set; }

        /// <summary>
        /// Uadp dataset ordering
        /// </summary>
        [DataMember(Name = "dataSetOrdering", Order = 2,
            EmitDefaultValue = false)]
        public DataSetOrderingType? DataSetOrdering { get; set; }

        /// <summary>
        /// Uadp Sampling offset
        /// </summary>
        [DataMember(Name = "samplingOffset", Order = 3,
            EmitDefaultValue = false)]
        public double? SamplingOffset { get; set; }

        /// <summary>
        /// Publishing offset for uadp messages
        /// </summary>
        [DataMember(Name = "publishingOffset", Order = 4,
            EmitDefaultValue = false)]
        public IReadOnlyList<double>? PublishingOffset { get; set; }

        /// <summary>
        /// Max messages per publish
        /// </summary>
        [DataMember(Name = "maxDataSetMessagesPerPublish", Order = 5,
            EmitDefaultValue = false)]
        public uint? MaxDataSetMessagesPerPublish { get; set; }

        /// <summary>
        /// Optional namespace format to use when serializing
        /// nodes and qualified names in responses.
        /// </summary>
        [DataMember(Name = "namespaceFormat", Order = 6,
            EmitDefaultValue = false)]
        public NamespaceFormat? NamespaceFormat { get; set; }
    }
}
