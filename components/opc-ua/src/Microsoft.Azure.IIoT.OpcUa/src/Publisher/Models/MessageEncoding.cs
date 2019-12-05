// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Message encoding
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum MessageEncoding {

        /// <summary>
        /// Json encoding
        /// </summary>
        Json,

        /// <summary>
        /// Uadp encoding
        /// </summary>
        Uadp
    }
}
