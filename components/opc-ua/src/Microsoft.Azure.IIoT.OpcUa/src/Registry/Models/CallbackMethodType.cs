// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Type of callback method to use
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CallbackMethodType {

        /// <summary>
        /// Call callback with get and query
        /// </summary>
        Get,

        /// <summary>
        /// Call callback with post, query, and payload
        /// </summary>
        Post,

        /// <summary>
        /// Call callback with put, query, and payload
        /// </summary>
        Put,

        /// <summary>
        /// Call callback with delete and query
        /// </summary>
        Delete
    }
}
