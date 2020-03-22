// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// Describes event fields to be published
    /// </summary>
    [DataContract]
    public class PublishedDataSetEventsApiModel {

        /// <summary>
        /// Identifier of event in the dataset.
        /// </summary>
        [DataMember(Name = "id",
            EmitDefaultValue = false)]
        public string Id { get; set; }

        /// <summary>
        /// Event notifier to subscribe to
        /// </summary>
        [DataMember(Name = "eventNotifier")]
        public string EventNotifier { get; set; }

        /// <summary>
        /// Browse path to event notifier node (Publisher extension)
        /// </summary>
        [DataMember(Name = "browsePath",
            EmitDefaultValue = false)]
        public string[] BrowsePath { get; set; }

        /// <summary>
        /// Event fields to select
        /// </summary>
        [DataMember(Name = "selectedFields")]
        public List<SimpleAttributeOperandApiModel> SelectedFields { get; set; }

        /// <summary>
        /// Filter to use to select event fields
        /// </summary>
        [DataMember(Name = "filter")]
        public ContentFilterApiModel Filter { get; set; }

        /// <summary>
        /// Queue size (Publisher extension)
        /// </summary>
        [DataMember(Name = "queueSize",
            EmitDefaultValue = false)]
        public uint? QueueSize { get; set; }

        /// <summary>
        /// Discard new values if queue is full (Publisher extension)
        /// </summary>
        [DataMember(Name = "discardNew",
            EmitDefaultValue = false)]
        public bool? DiscardNew { get; set; }

        /// <summary>
        /// Monitoring mode (Publisher extension)
        /// </summary>
        [DataMember(Name = "monitoringMode",
            EmitDefaultValue = false)]
        public MonitoringMode? MonitoringMode { get; set; }

        /// <summary>
        /// Node in dataset writer that triggers reporting
        /// (Publisher extension)
        /// </summary>
        [DataMember(Name = "triggerId",
            EmitDefaultValue = false)]
        public string TriggerId { get; set; }
    }
}