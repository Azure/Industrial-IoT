// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.History.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using System.Collections.Generic;

    /// <summary>
    /// Replace historic events
    /// </summary>
    public class ReplaceEventsDetailsModel {

        /// <summary>
        /// The filter to use to select the events
        /// </summary>
        public ContentFilterModel Filter { get; set; }

        /// <summary>
        /// The new events to replace
        /// </summary>
        public List<HistoricEventModel> Events { get; set; }
    }
}
