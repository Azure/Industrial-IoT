// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Diagnostics configuration
    /// </summary>
    public class DiagnosticsApiModel {

        /// <summary>
        /// Requested level of response diagnostics.
        /// (default: None)
        /// </summary>
        [JsonProperty(PropertyName = "level",
            NullValueHandling = NullValueHandling.Ignore)]
        public DiagnosticsLevel? Level { get; set; }

        /// <summary>
        /// Client audit log entry.
        /// (default: client generated)
        /// </summary>
        [JsonProperty(PropertyName = "auditId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string AuditId { get; set; }

        /// <summary>
        /// Timestamp of request.
        /// (default: client generated)
        /// </summary>
        [JsonProperty(PropertyName = "timeStamp",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? TimeStamp { get; set; }
    }
}
