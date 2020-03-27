// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Clients.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Pub/sub job description
    /// </summary>
    public class DataSetWriterApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public DataSetWriterApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public DataSetWriterApiModel(DataSetWriterModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            DataSetWriterId = model.DataSetWriterId;
            DataSet = model.DataSet == null ? null :
                new PublishedDataSetApiModel(model.DataSet);
            DataSetFieldContentMask = model.DataSetFieldContentMask;
            MessageSettings = model.MessageSettings == null ? null :
                new DataSetWriterMessageSettingsApiModel(model.MessageSettings);
            KeyFrameInterval = model.KeyFrameInterval;
            DataSetMetaDataSendInterval = model.DataSetMetaDataSendInterval;
            KeyFrameCount = model.KeyFrameCount;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public DataSetWriterModel ToServiceModel() {
            return new DataSetWriterModel {
                DataSetWriterId = DataSetWriterId,
                DataSet = DataSet?.ToServiceModel(),
                DataSetFieldContentMask = DataSetFieldContentMask,
                DataSetMetaDataSendInterval = DataSetMetaDataSendInterval,
                KeyFrameCount = KeyFrameCount,
                KeyFrameInterval = KeyFrameInterval,
                MessageSettings = MessageSettings?.ToServiceModel()
            };
        }

        /// <summary>
        /// Dataset writer id
        /// </summary>
        [JsonProperty(PropertyName = "dataSetWriterId")]
        public string DataSetWriterId { get; set; }

        /// <summary>
        /// Published dataset inline definition
        /// </summary>
        [JsonProperty(PropertyName = "dataSet",
            NullValueHandling = NullValueHandling.Ignore)]
        public PublishedDataSetApiModel DataSet { get; set; }

        /// <summary>
        /// Dataset field content mask
        /// </summary>
        [JsonProperty(PropertyName = "dataSetFieldContentMask",
            NullValueHandling = NullValueHandling.Ignore)]
        public DataSetFieldContentMask? DataSetFieldContentMask { get; set; }

        /// <summary>
        /// Data set message settings
        /// </summary>
        [JsonProperty(PropertyName = "messageSettings",
            NullValueHandling = NullValueHandling.Ignore)]
        public DataSetWriterMessageSettingsApiModel MessageSettings { get; set; }

        /// <summary>
        /// Keyframe count
        /// </summary>
        [JsonProperty(PropertyName = "keyFrameCount",
            NullValueHandling = NullValueHandling.Ignore)]
        public uint? KeyFrameCount { get; set; }

        /// <summary>
        /// Or keyframe timer interval (publisher extension)
        /// </summary>
        [JsonProperty(PropertyName = "keyFrameInterval",
            NullValueHandling = NullValueHandling.Ignore)]
        public TimeSpan? KeyFrameInterval { get; set; }

        /// <summary>
        /// Metadata message sending interval (publisher extension)
        /// </summary>
        [JsonProperty(PropertyName = "dataSetMetaDataSendInterval",
            NullValueHandling = NullValueHandling.Ignore)]
        public TimeSpan? DataSetMetaDataSendInterval { get; set; }
    }
}