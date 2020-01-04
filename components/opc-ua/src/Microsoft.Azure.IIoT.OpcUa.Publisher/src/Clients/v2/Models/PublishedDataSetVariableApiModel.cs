// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Clients.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Dataset Field model
    /// </summary>
    public class PublishedDataSetVariableApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public PublishedDataSetVariableApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public PublishedDataSetVariableApiModel(PublishedDataSetVariableModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            Id = model.Id;
            PublishedVariableNodeId = model.PublishedVariableNodeId;
            BrowsePath = model.BrowsePath;
            Attribute = model.Attribute;
            DataChangeFilter = model.DataChangeFilter;
            DeadbandType = model.DeadbandType;
            DeadbandValue = model.DeadbandValue;
            DiscardNew = model.DiscardNew;
            IndexRange = model.IndexRange;
            MetaDataProperties = model.MetaDataProperties?.ToList();
            QueueSize = model.QueueSize;
            SamplingInterval = model.SamplingInterval;
            MonitoringMode = model.MonitoringMode;
            SubstituteValue = model.SubstituteValue?.DeepClone();
            TriggerId = model.TriggerId;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public PublishedDataSetVariableModel ToServiceModel() {
            return new PublishedDataSetVariableModel {
                Id = Id,
                PublishedVariableNodeId = PublishedVariableNodeId,
                BrowsePath = BrowsePath,
                Attribute = Attribute,
                DataChangeFilter = DataChangeFilter,
                DeadbandType = DeadbandType,
                DeadbandValue = DeadbandValue,
                DiscardNew = DiscardNew,
                IndexRange = IndexRange,
                MonitoringMode = MonitoringMode,
                MetaDataProperties = MetaDataProperties?.ToList(),
                QueueSize  = QueueSize,
                SamplingInterval = SamplingInterval,
                TriggerId = TriggerId,
                SubstituteValue = SubstituteValue?.DeepClone()
            };
        }

        /// <summary>
        /// Identifier of variable in the dataset.
        /// </summary>
        [JsonProperty(PropertyName = "id",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }

        /// <summary>
        /// Node id
        /// </summary>
        [JsonProperty(PropertyName = "publishedVariableNodeId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string PublishedVariableNodeId { get; set; }

        /// <summary>
        /// An optional component path from the node identified by
        /// PublishedVariableNodeId to the actual node to publish
        /// (Publisher extension).
        /// </summary>
        [JsonProperty(PropertyName = "browsePath",
            NullValueHandling = NullValueHandling.Ignore)]
        public string[] BrowsePath { get; set; }

        /// <summary>
        /// Default is <see cref="NodeAttribute.Value"/>.
        /// </summary>
        [JsonProperty(PropertyName = "attribute",
            NullValueHandling = NullValueHandling.Ignore)]
        public NodeAttribute? Attribute { get; set; }

        /// <summary>
        /// Index range
        /// </summary>
        [JsonProperty(PropertyName = "indexRange",
            NullValueHandling = NullValueHandling.Ignore)]
        public string IndexRange { get; set; }

        /// <summary>
        /// Sampling Interval - default is best effort
        /// </summary>
        [JsonProperty(PropertyName = "samplingInterval",
            NullValueHandling = NullValueHandling.Ignore)]
        public TimeSpan? SamplingInterval { get; set; }

        /// <summary>
        /// Data change filter
        /// </summary>
        [JsonProperty(PropertyName = "dataChangeFilter",
            NullValueHandling = NullValueHandling.Ignore)]
        public DataChangeTriggerType? DataChangeFilter { get; set; }

        /// <summary>
        /// Deadband type
        /// </summary>
        [JsonProperty(PropertyName = "deadbandType",
            NullValueHandling = NullValueHandling.Ignore)]
        public DeadbandType? DeadbandType { get; set; }

        /// <summary>
        /// Deadband value
        /// </summary>
        [JsonProperty(PropertyName = "deadbandValue",
            NullValueHandling = NullValueHandling.Ignore)]
        public double? DeadbandValue { get; set; }

        /// <summary>
        /// Substitution value for empty results
        /// </summary>
        [JsonProperty(PropertyName = "substituteValue",
            NullValueHandling = NullValueHandling.Ignore)]
        public JToken SubstituteValue { get; set; }

        /// <summary>
        /// MetaData properties qualified names.
        /// </summary>
        [JsonProperty(PropertyName = "metaDataProperties",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<string> MetaDataProperties { get; set; }

        /// <summary>
        /// Monitoring mode (Publisher extension)
        /// </summary>
        [JsonProperty(PropertyName = "monitoringMode",
            NullValueHandling = NullValueHandling.Ignore)]
        public MonitoringItemMode? MonitoringMode { get; set; }

        /// <summary>
        /// Queue size (Publisher extension)
        /// </summary>
        [JsonProperty(PropertyName = "queueSize",
            NullValueHandling = NullValueHandling.Ignore)]
        public uint? QueueSize { get; set; }

        /// <summary>
        /// Discard new values if queue is full (Publisher extension)
        /// </summary>
        [JsonProperty(PropertyName = "discardNew",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? DiscardNew { get; set; }

        /// <summary>
        /// Node in dataset writer that triggers reporting
        /// (Publisher extension)
        /// </summary>
        [JsonProperty(PropertyName = "triggerId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string TriggerId { get; set; }
    }
}