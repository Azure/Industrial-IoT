// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.History.Models {
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;

    /// <summary>
    /// Update historic events
    /// </summary>
    public class UpdateEventsDetailsModel {

        /// <summary>
        /// Whether to perform insert or replacement
        /// </summary>
        public HistoryUpdateOperation PerformInsertReplace { get; set; }

        /// <summary>
        /// The filter to use to select the events
        /// </summary>
        public JToken Filter { get; set; }

        /// <summary>
        /// The new events to insert or replace
        /// </summary>
        public List<HistoricEventModel> EventData { get; set; }
    }
}
