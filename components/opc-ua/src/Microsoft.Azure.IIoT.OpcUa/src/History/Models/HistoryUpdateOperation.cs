// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.History.Models {
    /// <summary>
    /// History update type
    /// </summary>
    public enum HistoryUpdateOperation {

        /// <summary>
        /// Insert
        /// </summary>
        Insert = 1,

        /// <summary>
        /// Replace
        /// </summary>
        Replace,

        /// <summary>
        /// Update
        /// </summary>
        Update,

        /// <summary>
        /// Delete
        /// </summary>
        Delete,
    }
}
