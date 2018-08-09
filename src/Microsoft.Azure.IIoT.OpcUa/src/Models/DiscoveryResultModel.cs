// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Models {
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Result of a discovery run - part of last event element
    /// in batch
    /// </summary>
    public class DiscoveryResultModel {

        /// <summary>
        /// Id of discovery request
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Configuration used during discovery
        /// </summary>
        public DiscoveryConfigModel DiscoveryConfig { get; set; }

        /// <summary>
        /// If true, only register, do not unregister based
        /// on these events.
        /// </summary>
        public bool? RegisterOnly { get; set; }

        /// <summary>
        /// If discovery failed, diagnostic information
        /// </summary>
        public JToken Diagnostics { get; set; }
    }
}
