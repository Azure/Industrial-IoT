// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// Writer group message configuration
    /// </summary>
    [DataContract]
    public class WriterGroupMessageSettingsApiModel {

        /// <summary>
        /// Network message content
        /// </summary>
        [DataMember(Name = "networkMessageContentMask",
            EmitDefaultValue = false)]
        public NetworkMessageContentMask? NetworkMessageContentMask { get; set; }

        /// <summary>
        /// Group version
        /// </summary>
        [DataMember(Name = "groupVersion",
            EmitDefaultValue = false)]
        public uint? GroupVersion { get; set; }

        /// <summary>
        /// Uadp dataset ordering
        /// </summary>
        [DataMember(Name = "dataSetOrdering",
            EmitDefaultValue = false)]
        public DataSetOrderingType? DataSetOrdering { get; set; }

        /// <summary>
        /// Uadp Sampling offset
        /// </summary>
        [DataMember(Name = "samplingOffset",
            EmitDefaultValue = false)]
        public double? SamplingOffset { get; set; }

        /// <summary>
        /// Publishing offset for uadp messages
        /// </summary>
        [DataMember(Name = "publishingOffset",
            EmitDefaultValue = false)]
        public List<double> PublishingOffset { get; set; }
    }
}