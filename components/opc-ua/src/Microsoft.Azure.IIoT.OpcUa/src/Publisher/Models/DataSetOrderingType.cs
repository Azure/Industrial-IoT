// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Ordering model
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DataSetOrderingType {

        /// <summary>
        /// Ascending writer id
        /// </summary>
        AscendingWriterId = 1,

        /// <summary>
        /// Single
        /// </summary>
        AscendingWriterIdSingle = 2,
    }
}
