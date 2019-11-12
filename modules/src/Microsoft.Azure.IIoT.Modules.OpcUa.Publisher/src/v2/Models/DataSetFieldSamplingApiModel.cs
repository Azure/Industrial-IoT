// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Monitored item
    /// </summary>
    public class DataSetFieldSamplingApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public DataSetFieldSamplingApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public DataSetFieldSamplingApiModel(DataSetFieldSamplingModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            DataChangeFilter = model.DataChangeFilter;
            DeadBandType = model.DeadBandType;
            DeadBandValue = model.DeadBandValue;
            DiscardNew = model.DiscardNew;
            QueueSize = model.QueueSize;
            SamplingInterval = model.SamplingInterval;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public DataSetFieldSamplingModel ToServiceModel() {
            return new DataSetFieldSamplingModel {
                DataChangeFilter = DataChangeFilter,
                DeadBandType = DeadBandType,
                DeadBandValue = DeadBandValue,
                DiscardNew = DiscardNew,
                QueueSize = QueueSize,
                SamplingInterval = SamplingInterval,
            };
        }

        /// <summary>
        /// Sampling interval
        /// </summary>
        [JsonProperty(PropertyName = "samplingInterval",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? SamplingInterval { get; set; }

        /// <summary>
        /// Queue size
        /// </summary>
        [JsonProperty(PropertyName = "queueSize",
            NullValueHandling = NullValueHandling.Ignore)]
        public uint? QueueSize { get; set; }

        /// <summary>
        /// Discard new values if queue is full
        /// </summary>
        [JsonProperty(PropertyName = "discardNew",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? DiscardNew { get; set; }

        /// <summary>
        /// Data change filter
        /// </summary>
        [JsonProperty(PropertyName = "dataChangeFilter",
            NullValueHandling = NullValueHandling.Ignore)]
        public DataChangeFilterType? DataChangeFilter { get; set; }

        /// <summary>
        /// Dead band
        /// </summary>
        [JsonProperty(PropertyName = "deadBandType",
            NullValueHandling = NullValueHandling.Ignore)]
        public DeadbandType? DeadBandType { get; set; }

        /// <summary>
        /// Dead band value
        /// </summary>
        [JsonProperty(PropertyName = "deadBandValue",
            NullValueHandling = NullValueHandling.Ignore)]
        public double? DeadBandValue { get; set; }
    }
}