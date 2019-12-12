// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.History.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System;

    /// <summary>
    /// Read event data
    /// </summary>
    public class ReadEventsDetailsModel {

        /// <summary>
        /// Start time to read from
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// End time to read to
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Number of events to read
        /// </summary>
        public uint? NumEvents { get; set; }

        /// <summary>
        /// The filter to use to select the event fields
        /// </summary>
        public EventFilterModel Filter { get; set; }
    }
}
