// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Publisher event type
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PublisherEventType {

        /// <summary>
        /// New
        /// </summary>
        New,

        /// <summary>
        /// Updated
        /// </summary>
        Updated,

        /// <summary>
        /// Deleted
        /// </summary>
        Deleted,
    }
}