// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using System.Runtime.Serialization;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Insert historic events
    /// </summary>
    [DataContract]
    public class InsertEventsDetailsApiModel {

        /// <summary>
        /// The filter to use to select the events
        /// </summary>
        [DataMember(Name = "filter", Order = 0,
            EmitDefaultValue = false)]
        public EventFilterApiModel Filter { get; set; }

        /// <summary>
        /// The new events to insert
        /// </summary>
        [DataMember(Name = "events", Order = 1)]
        [Required]
        public List<HistoricEventApiModel> Events { get; set; }
    }
}
