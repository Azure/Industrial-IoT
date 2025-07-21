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
        /// Dataset writer identifier.
        /// </summary>
        [DataMember(Name = "id", Order = 0)]
        public required string Id { get; set; }

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
        public DataSetFieldContentFlags? DataSetFieldContentMask { get; set; }

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
        /// Dataset writer name.
        /// </summary>
        [DataMember(Name = "dataSetWriterName", Order = 5)]
        public string? DataSetWriterName { get; set; }

        /// <summary>
        /// Metadata message sending interval
        /// </summary>
        [DataMember(Name = "metaDataUpdateTime", Order = 6,
            EmitDefaultValue = false)]
        public TimeSpan? MetaDataUpdateTime { get; set; }

        /// <summary>
        /// Metadata queue settings the writer should use to publish
        /// metadata messages to.
        /// </summary>
        [DataMember(Name = "metaData", Order = 7,
            EmitDefaultValue = false)]
        public PublishingQueueSettingsModel? MetaData { get; set; }

        /// <summary>
        /// Queue settings writer should use to publish messages
        /// to. Overrides the writer group queue settings.
        /// (Publisher extension)
        /// </summary>
        [DataMember(Name = "publishing", Order = 8,
            EmitDefaultValue = false)]
        public PublishingQueueSettingsModel? Publishing { get; set; }
    }
}
