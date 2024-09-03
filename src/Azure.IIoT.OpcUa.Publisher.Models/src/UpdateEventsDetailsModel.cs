// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Insert, upsert or replace historic events
    /// </summary>
    [DataContract]
    public sealed record class UpdateEventsDetailsModel
    {
        /// <summary>
        /// The filter to use to select the events
        /// </summary>
        [DataMember(Name = "filter", Order = 0,
            EmitDefaultValue = false)]
        public EventFilterModel? Filter { get; set; }

        /// <summary>
        /// The new events to insert
        /// </summary>
        [DataMember(Name = "events", Order = 1)]
        [Required]
        public required IReadOnlyList<HistoricEventModel> Events { get; set; }
    }
}
