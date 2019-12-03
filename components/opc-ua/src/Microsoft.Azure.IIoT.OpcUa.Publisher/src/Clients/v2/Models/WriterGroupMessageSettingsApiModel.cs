// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Clients.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Writer group message configuration
    /// </summary>
    public class WriterGroupMessageSettingsApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public WriterGroupMessageSettingsApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public WriterGroupMessageSettingsApiModel(WriterGroupMessageSettingsModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            NetworkMessageContentMask = model.NetworkMessageContentMask;
            DataSetOrdering = model.DataSetOrdering;
            GroupVersion = model.GroupVersion;
            PublishingOffset = model.PublishingOffset;
            SamplingOffset = model.SamplingOffset;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public WriterGroupMessageSettingsModel ToServiceModel() {
            return new WriterGroupMessageSettingsModel {
                NetworkMessageContentMask = NetworkMessageContentMask,
                DataSetOrdering = DataSetOrdering,
                GroupVersion = GroupVersion,
                PublishingOffset = PublishingOffset,
                SamplingOffset = SamplingOffset
            };
        }

        /// <summary>
        /// Network message content
        /// </summary>
        [JsonProperty(PropertyName = "networkMessageContentMask",
            NullValueHandling = NullValueHandling.Ignore)]
        public NetworkMessageContentMask? NetworkMessageContentMask { get; set; }

        /// <summary>
        /// Group version
        /// </summary>
        [JsonProperty(PropertyName = "groupVersion",
            NullValueHandling = NullValueHandling.Ignore)]
        public uint? GroupVersion { get; set; }

        /// <summary>
        /// Uadp dataset ordering
        /// </summary>
        [JsonProperty(PropertyName = "dataSetOrdering",
            NullValueHandling = NullValueHandling.Ignore)]
        public DataSetOrderingType? DataSetOrdering { get; set; }

        /// <summary>
        /// Uadp Sampling offset
        /// </summary>
        [JsonProperty(PropertyName = "samplingOffset",
            NullValueHandling = NullValueHandling.Ignore)]
        public double? SamplingOffset { get; set; }

        /// <summary>
        /// Publishing offset for uadp messages
        /// </summary>
        [JsonProperty(PropertyName = "publishingOffset",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<double> PublishingOffset { get; set; }
    }
}