// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Monitored item sample encoding
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum MonitoredItemMessageEncoding {

        /// <summary>
        /// Encode as json
        /// </summary>
        Json
    }
}
