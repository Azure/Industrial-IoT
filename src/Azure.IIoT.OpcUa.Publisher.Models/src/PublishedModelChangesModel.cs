// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Describes how model changes are published
    /// </summary>
    [DataContract]
    public sealed record class PublishedModelChangesModel
    {
        /// <summary>
        /// Identifier of event in the dataset.
        /// </summary>
        [DataMember(Name = "id", Order = 0,
            EmitDefaultValue = false)]
        public string? Id { get; set; }

        /// <summary>
        /// Event notifier to subscribe to (if not server)
        /// </summary>
        [DataMember(Name = "eventNotifier", Order = 1,
            EmitDefaultValue = false)]
        public string? EventNotifier { get; set; }

        /// <summary>
        /// Rebrowse period
        /// </summary>
        [DataMember(Name = "rebrowsePeriod", Order = 2,
            EmitDefaultValue = false)]
        public TimeSpan? RebrowsePeriod { get; set; }

        /// <summary>
        /// Root node to monitor (if not root folder)
        /// </summary>
        [DataMember(Name = "startNodeId", Order = 3,
            EmitDefaultValue = false)]
        public string? StartNodeId { get; set; }

        /// <summary>
        /// Event name
        /// </summary>
        [DataMember(Name = "publishedEventName", Order = 4,
            EmitDefaultValue = false)]
        public string? PublishedEventName { get; set; }
    }
}
