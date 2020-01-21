// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.History.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System.Collections.Generic;

    /// <summary>
    /// Insert historic events
    /// </summary>
    public class InsertEventsDetailsModel {

        /// <summary>
        /// The filter to use to select the events
        /// </summary>
        public EventFilterModel Filter { get; set; }

        /// <summary>
        /// The new events to insert
        /// </summary>
        public List<HistoricEventModel> Events { get; set; }
    }
}
