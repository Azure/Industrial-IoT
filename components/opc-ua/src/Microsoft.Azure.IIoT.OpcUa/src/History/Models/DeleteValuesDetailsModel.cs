// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.History.Models {
    using System;

    /// <summary>
    /// Delete raw data
    /// </summary>
    public class DeleteValuesDetailsModel {

        /// <summary>
        /// Start time
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// End time to delete until
        /// </summary>
        public DateTime? EndTime { get; set; }
    }
}
