// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.History.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// History update type
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
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
