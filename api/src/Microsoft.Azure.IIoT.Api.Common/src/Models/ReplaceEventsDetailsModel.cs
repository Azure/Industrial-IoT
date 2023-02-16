// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Models {
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Replace historic events
    /// </summary>
    [DataContract]
    public record class ReplaceEventsDetailsModel {

        /// <summary>
        /// The filter to use to select the events
        /// </summary>
        [DataMember(Name = "filter", Order = 0,
            EmitDefaultValue = false)]
        public EventFilterModel Filter { get; set; }

        /// <summary>
        /// The new events to replace
        /// </summary>
        [DataMember(Name = "events", Order = 1)]
        [Required]
        public List<HistoricEventModel> Events { get; set; }
    }
}
