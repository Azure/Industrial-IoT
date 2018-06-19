// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.OpcUa.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Discovery mode to use
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DiscoveryMode {

        /// <summary>
        /// Disable discovery
        /// </summary>
        Off,

        /// <summary>
        /// Use local discovery server
        /// </summary>
        Local,

        /// <summary>
        /// Find and use all LDS in all connected networks
        /// </summary>
        Network,

        /// <summary>
        /// Perform a deep scan of all networks.
        /// </summary>
        Scan
    }
}
