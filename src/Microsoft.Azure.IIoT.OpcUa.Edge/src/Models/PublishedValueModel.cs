// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;

    /// <summary>
    /// Publish request
    /// </summary>
    public class PublishedValueModel {

        /// <summary>
        /// Published node
        /// </summary>
        [JsonProperty]
        public string NodeId { get; set; }

        /// <summary>
        /// Value
        /// </summary>
        [JsonProperty]
        public JToken Value { get; set; }

        /// <summary>
        /// Source pico
        /// </summary>
        [JsonProperty]
        public ushort? SourcePicoseconds { get; set; }

        /// <summary>
        /// Source timestamp
        /// </summary>
        [JsonProperty]
        public DateTime? SourceTimestamp { get; set; }

        /// <summary>
        /// Server pico
        /// </summary>
        [JsonProperty]
        public ushort? ServerPicoseconds { get; set; }

        /// <summary>
        /// Server timestamp
        /// </summary>
        [JsonProperty]
        public DateTime? ServerTimestamp { get; set; }

        /// <summary>
        /// Status code
        /// </summary>
        [JsonProperty]
        public uint? StatusCode { get; set; }

        /// <summary>
        /// Status string
        /// </summary>
        [JsonProperty]
        public string Status { get; set; }

        /// <summary>
        /// Display name
        /// </summary>
        [JsonProperty]
        public string DisplayName { get; set; }
    }
}
