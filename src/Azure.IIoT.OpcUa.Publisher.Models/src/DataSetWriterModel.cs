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
        /// Numeric index defining place in the writer group
        /// </summary>
        [DataMember(Name = "dataSetWriterId", Order = 0)]
        public required ushort DataSetWriterId { get; init; }

        /// <summary>
        /// Dataset writer identifier.
        /// </summary>
        [DataMember(Name = "id", Order = 1)]
        public required string Id { get; init; }

        /// <summary>
        /// Dataset writer name.
        /// </summary>
        [DataMember(Name = "dataSetWriterName", Order = 2)]
        public string? DataSetWriterName { get; init; }

        /// <summary>
        /// Published dataset inline definition
        /// </summary>
        [DataMember(Name = "dataSet", Order = 3,
            EmitDefaultValue = false)]
        public PublishedDataSetModel? DataSet { get; init; }

        /// <summary>
        /// Dataset field content mask
        /// </summary>
        [DataMember(Name = "dataSetFieldContentMask", Order = 4,
            EmitDefaultValue = false)]
        public DataSetFieldContentMask? DataSetFieldContentMask { get; init; }

        /// <summary>
        /// Data set message settings
        /// </summary>
        [DataMember(Name = "messageSettings", Order = 5,
            EmitDefaultValue = false)]
        public DataSetWriterMessageSettingsModel? MessageSettings { get; init; }

        /// <summary>
        /// Keyframe count
        /// </summary>
        [DataMember(Name = "keyFrameCount", Order = 6,
            EmitDefaultValue = false)]
        public uint? KeyFrameCount { get; init; }

        /// <summary>
        /// Metadata message sending interval
        /// </summary>
        [DataMember(Name = "metaDataUpdateTime", Order = 7,
            EmitDefaultValue = false)]
        public TimeSpan? MetaDataUpdateTime { get; init; }

        /// <summary>
        /// Metadata queue settings the writer should use to publish
        /// metadata messages to.
        /// </summary>
        [DataMember(Name = "metaData", Order = 8,
            EmitDefaultValue = false)]
        public PublishingQueueSettingsModel? MetaData { get; init; }

        /// <summary>
        /// Queue settings writer should use to publish messages
        /// to. Overrides the writer group queue settings.
        /// (Publisher extension)
        /// </summary>
        [DataMember(Name = "publishing", Order = 9,
            EmitDefaultValue = false)]
        public PublishingQueueSettingsModel? Publishing { get; init; }

        /// <summary>
        /// Sets the current error state
        /// </summary>
        [DataMember(Name = "state", Order = 10,
            EmitDefaultValue = false)]
        public ServiceResultModel? State { get; init; }
    }
}
