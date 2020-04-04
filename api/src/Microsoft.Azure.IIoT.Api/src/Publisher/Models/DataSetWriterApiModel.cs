// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System.Runtime.Serialization;
    using System;

    /// <summary>
    /// Pub/sub job description
    /// </summary>
    [DataContract]
    public class DataSetWriterApiModel {

        /// <summary>
        /// Dataset writer id
        /// </summary>
        [DataMember(Name = "dataSetWriterId", Order = 0)]
        public string DataSetWriterId { get; set; }

        /// <summary>
        /// Published dataset inline definition
        /// </summary>
        [DataMember(Name = "dataSet", Order = 1,
            EmitDefaultValue = false)]
        public PublishedDataSetApiModel DataSet { get; set; }

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
        public DataSetWriterMessageSettingsApiModel MessageSettings { get; set; }

        /// <summary>
        /// Keyframe count
        /// </summary>
        [DataMember(Name = "keyFrameCount", Order = 4,
            EmitDefaultValue = false)]
        public uint? KeyFrameCount { get; set; }

        /// <summary>
        /// Or keyframe timer interval (publisher extension)
        /// </summary>
        [DataMember(Name = "keyFrameInterval", Order = 5,
            EmitDefaultValue = false)]
        public TimeSpan? KeyFrameInterval { get; set; }

        /// <summary>
        /// Metadata message sending interval (publisher extension)
        /// </summary>
        [DataMember(Name = "dataSetMetaDataSendInterval", Order = 6,
            EmitDefaultValue = false)]
        public TimeSpan? DataSetMetaDataSendInterval { get; set; }
    }
}