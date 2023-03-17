// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Data set writer description
    /// </summary>
    [DataContract]
    public sealed record class DataSetWriterModel
    {
        /// <summary>
        /// Dataset writer name.
        /// </summary>
        [DataMember(Name = "dataSetWriterName", Order = 0)]
        public string? DataSetWriterName { get; set; }

        /// <summary>
        /// Published dataset inline definition
        /// </summary>
        [DataMember(Name = "dataSet", Order = 1,
            EmitDefaultValue = false)]
        public PublishedDataSetModel? DataSet { get; set; }

        /// <summary>
        /// Dataset field content mask
        /// </summary>
        [DataMember(Name = "dataSetFieldContentMask", Order = 2,
            EmitDefaultValue = false)]
        public DataSetFieldContentMask? DataSetFieldContentMask { get; set; }

        /// <summary>
        /// Data set message settings
        /// </summary>
        [DataMember(Name = "messageSettings", Order = 3,
            EmitDefaultValue = false)]
        public DataSetWriterMessageSettingsModel? MessageSettings { get; set; }

        /// <summary>
        /// Keyframe count
        /// </summary>
        [DataMember(Name = "keyFrameCount", Order = 4,
            EmitDefaultValue = false)]
        public uint? KeyFrameCount { get; set; }

        /// <summary>
        /// Metadata message sending interval
        /// </summary>
        [DataMember(Name = "metaDataUpdateTime", Order = 6,
            EmitDefaultValue = false)]
        public TimeSpan? MetaDataUpdateTime { get; set; }
    }
}
