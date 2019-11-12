// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;

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
            Id = model.Id;
            ContentEncoding = model.ContentEncoding;
            DataSetContent = model.DataSetContent;
            DataSets = model.DataSets?
                .Select(d => new DataSetApiModel(d))
                .ToList();
            FieldContent = model.FieldContent;
            KeyframeMessageInterval = model.KeyframeMessageInterval;
            MetadataMessageInterval = model.MetadataMessageInterval;
            NetworkMessageContent = model.NetworkMessageContent;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public DataSetWriterModel ToServiceModel() {
            return new DataSetWriterModel {
                Id = Id,
                ContentEncoding = ContentEncoding,
                DataSetContent = DataSetContent,
                DataSets = DataSets?
                    .Select(d => d.ToServiceModel())
                    .ToList(),
                FieldContent = FieldContent,
                KeyframeMessageInterval = KeyframeMessageInterval,
                MetadataMessageInterval = MetadataMessageInterval,
                NetworkMessageContent = NetworkMessageContent,
            };
        }

        /// <summary>
        /// Dataset writer id
        /// </summary>
        [JsonProperty(PropertyName = "Id")]
        public string Id { get; set; }

        /// <summary>
        /// Datasets to publish
        /// </summary>
        [JsonProperty(PropertyName = "dataSets")]
        public List<DataSetApiModel> DataSets { get; set; }

        /// <summary>
        /// Keyframe message interval
        /// </summary>
        [JsonProperty(PropertyName = "keyframeMessageInterval",
            NullValueHandling = NullValueHandling.Ignore)]
        public TimeSpan? KeyframeMessageInterval { get; set; }

        /// <summary>
        /// Metadata message interval
        /// </summary>
        [JsonProperty(PropertyName = "metadataMessageInterval",
            NullValueHandling = NullValueHandling.Ignore)]
        public TimeSpan? MetadataMessageInterval { get; set; }

        /// <summary>
        /// Content type to use
        /// </summary>
        [JsonProperty(PropertyName = "contentEncoding",
            NullValueHandling = NullValueHandling.Ignore)]
        public NetworkMessageEncoding? ContentEncoding { get; set; }

        /// <summary>
        /// Network message content
        /// </summary>
        [JsonProperty(PropertyName = "networkMessageContent",
            NullValueHandling = NullValueHandling.Ignore)]
        public NetworkMessageContentMask? NetworkMessageContent { get; set; }

        /// <summary>
        /// Dataset message content
        /// </summary>
        [JsonProperty(PropertyName = "dataSetContent",
            NullValueHandling = NullValueHandling.Ignore)]
        public DataSetContentMask? DataSetContent { get; set; }

        /// <summary>
        /// Field content
        /// </summary>
        [JsonProperty(PropertyName = "fieldContent",
            NullValueHandling = NullValueHandling.Ignore)]
        public DataSetFieldContentMask? FieldContent { get; set; }
    }
}