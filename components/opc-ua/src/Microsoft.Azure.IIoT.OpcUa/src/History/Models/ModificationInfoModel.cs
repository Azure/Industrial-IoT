// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.History.Models {
    using System;

    /// <summary>
    /// Modification information
    /// </summary>
    public class ModificationInfoModel {

        /// <summary>
        /// Modification time
        /// </summary>
        public DateTime? ModificationTime { get; set; }

        /// <summary>
        /// Operation
        /// </summary>
        public HistoryUpdateOperation? UpdateType { get; set; }

        /// <summary>
        /// User who made the change
        /// </summary>
        public string UserName { get; set; }
    }
}
