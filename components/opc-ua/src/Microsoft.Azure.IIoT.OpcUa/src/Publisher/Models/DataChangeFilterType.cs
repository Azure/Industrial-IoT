// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Data change filter
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DataChangeFilterType {

        /// <summary>
        /// Status
        /// </summary>
        Status,

        /// <summary>
        /// Status value
        /// </summary>
        StatusValue,

        /// <summary>
        /// Status value and timestamp
        /// </summary>
        StatusValueTimestamp
    }
}