// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Direction to browse
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum BrowseDirection {

        /// <summary>
        /// Forward
        /// </summary>
        Forward,

        /// <summary>
        /// Backward
        /// </summary>
        Backward,

        /// <summary>
        /// Both directions
        /// </summary>
        Both
    }
}
