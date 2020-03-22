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
        [DataMember(Name = "DataSetWriterId")]
        public string DataSetWriterId { get; set; }

        /// <summary>
        /// Published dataset inline definition
        /// </summary>
        [DataMember(Name = "dataSet",
            EmitDefaultValue = false)]
        public PublishedDataSetApiModel DataSet { get; set; }

        /// <summary>
        /// Dataset field content mask
        /// </summary>
        [DataMember(Name = "dataSetFieldContentMask",
            EmitDefaultValue = false)]
        public DataSetFieldContentMask? DataSetFieldContentMask { get; set; }

        /// <summary>
        /// Data set message settings
        /// </summary>
        [DataMember(Name = "messageSettings",
            EmitDefaultValue = false)]
        public DataSetWriterMessageSettingsApiModel MessageSettings { get; set; }

        /// <summary>
        /// Keyframe count
        /// </summary>
        [DataMember(Name = "keyFrameCount",
            EmitDefaultValue = false)]
        public uint? KeyFrameCount { get; set; }

        /// <summary>
        /// Or keyframe timer interval (publisher extension)
        /// </summary>
        [DataMember(Name = "keyFrameInterval",
            EmitDefaultValue = false)]
        public TimeSpan? KeyFrameInterval { get; set; }

        /// <summary>
        /// Metadata message sending interval (publisher extension)
        /// </summary>
        [DataMember(Name = "dataSetMetaDataSendInterval",
            EmitDefaultValue = false)]
        public TimeSpan? DataSetMetaDataSendInterval { get; set; }
    }
}