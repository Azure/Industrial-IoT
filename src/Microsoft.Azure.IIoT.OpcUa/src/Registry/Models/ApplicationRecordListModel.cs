// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Application query response
    /// </summary>
    public sealed class ApplicationRecordListModel {

        /// <summary>
        /// Found applications
        /// </summary>
        public List<ApplicationRecordModel> Applications { get; set; }

        /// <summary>
        /// Last counter reset
        /// </summary>
        public DateTime LastCounterResetTime { get; set; }

        /// <summary>
        /// Next record id
        /// </summary>
        public uint NextRecordId { get; set; }
    }
}
