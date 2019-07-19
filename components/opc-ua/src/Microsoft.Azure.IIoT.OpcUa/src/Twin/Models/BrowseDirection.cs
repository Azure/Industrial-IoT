// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Direction to browse
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum BrowseDirection {

        /// <summary>
        /// Browse forward (default)
        /// </summary>
        Forward,

        /// <summary>
        /// Browse backward
        /// </summary>
        Backward,

        /// <summary>
        /// Browse both directions
        /// </summary>
        Both
    }
}
