// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Describes event fields to be published
    /// </summary>
    public class PublishedDataSetEventModel {

        /// <summary>
        /// Identifier of event in the dataset.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Event notifier to subscribe to (or start node)
        /// </summary>
        public string EventNotifier { get; set; }

        /// <summary>
        /// Browse path to event notifier node (Publisher extension)
        /// </summary>
        public string[] BrowsePath { get; set; }

        /// <summary>
        /// Fields to select
        /// </summary>
        public List<SimpleAttributeOperandModel> SelectClauses { get; set; }

        /// <summary>
        /// Filter to use
        /// </summary>
        public ContentFilterModel WhereClause { get; set; }

        /// <summary>
        /// Queue size (Publisher extension)
        /// </summary>
        public uint? QueueSize { get; set; }

        /// <summary>
        /// Discard new values if queue is full (Publisher extension)
        /// </summary>
        public bool? DiscardNew { get; set; }

        /// <summary>
        /// Monitoring mode (Publisher extension)
        /// </summary>
        public MonitoringMode? MonitoringMode { get; set; }

        /// <summary>
        /// Node in dataset writer that triggers reporting
        /// (Publisher extension)
        /// </summary>
        public string TriggerId { get; set; }

        /// <summary>
        /// Pending alarm settings
        /// </summary>
        public ConditionHandlingOptionsModel ConditionHandling { get; set; }

        /// <summary>
        /// Simple event Type id
        /// </summary>
        public string TypeDefinitionId { get; set; }
    }
}