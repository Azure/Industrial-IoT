// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Clients.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Describes event fields to be published
    /// </summary>
    public class PublishedDataSetEventsApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public PublishedDataSetEventsApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public PublishedDataSetEventsApiModel(PublishedDataSetEventsModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            Id = model.Id;
            Filter = model.Filter == null ? null :
                new ContentFilterApiModel(model.Filter);
            DiscardNew = model.DiscardNew;
            EventNotifier = model.EventNotifier;
            BrowsePath = model.BrowsePath;
            QueueSize = model.QueueSize;
            SelectedFields = model.SelectedFields?
                .Select(f => new SimpleAttributeOperandApiModel(f))
                .ToList();
            TriggerId = model.TriggerId;
            MonitoringMode = model.MonitoringMode;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public PublishedDataSetEventsModel ToServiceModel() {
            return new PublishedDataSetEventsModel {
                Id = Id,
                DiscardNew = DiscardNew,
                EventNotifier = EventNotifier,
                BrowsePath = BrowsePath,
                Filter = Filter?.ToServiceModel(),
                QueueSize = QueueSize,
                MonitoringMode = MonitoringMode,
                TriggerId = TriggerId,
                SelectedFields = SelectedFields?
                    .Select(f => f.ToServiceModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Identifier of event in the dataset.
        /// </summary>
        [JsonProperty(PropertyName = "id",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }

        /// <summary>
        /// Event notifier to subscribe to
        /// </summary>
        [JsonProperty(PropertyName = "eventNotifier")]
        public string EventNotifier { get; set; }

        /// <summary>
        /// Browse path to event notifier node (Publisher extension)
        /// </summary>
        [JsonProperty(PropertyName = "browsePath",
            NullValueHandling = NullValueHandling.Ignore)]
        public string[] BrowsePath { get; set; }

        /// <summary>
        /// Event fields to select
        /// </summary>
        [JsonProperty(PropertyName = "selectedFields")]
        public List<SimpleAttributeOperandApiModel> SelectedFields { get; set; }

        /// <summary>
        /// Filter to use to select event fields
        /// </summary>
        [JsonProperty(PropertyName = "filter")]
        public ContentFilterApiModel Filter { get; set; }

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
        /// Monitoring mode (Publisher extension)
        /// </summary>
        [JsonProperty(PropertyName = "monitoringMode",
            NullValueHandling = NullValueHandling.Ignore)]
        public MonitoringItemMode? MonitoringMode { get; set; }

        /// <summary>
        /// Node in dataset writer that triggers reporting
        /// (Publisher extension)
        /// </summary>
        [JsonProperty(PropertyName = "triggerId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string TriggerId { get; set; }
    }
}