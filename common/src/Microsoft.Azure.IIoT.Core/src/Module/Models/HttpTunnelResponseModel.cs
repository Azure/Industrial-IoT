// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Models {
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// Tunneled message
    /// </summary>
    [DataContract]
    public class HttpTunnelResponseModel {

        /// <summary>
        /// Message contains discover requests
        /// </summary>
        public const string SchemaName =
            "application/x-http-tunnel-response-v1";

        /// <summary>
        /// Request id
        /// </summary>
        [DataMember(Name = "requestId", Order = 0)]
        public string RequestId { get; set; }

        /// <summary>
        /// Headers
        /// </summary>
        [DataMember(Name = "headers", Order = 1,
            EmitDefaultValue = false)]
        public Dictionary<string, List<string>> Headers { get; set; }

        /// <summary>
        /// Payload chunk or null for upload responses and
        /// response continuation requests.
        /// </summary>
        [DataMember(Name = "payload", Order = 2,
            EmitDefaultValue = false)]
        public byte[] Payload { get; set; }

        /// <summary>
        /// Status code of call - in first response chunk.
        /// </summary>
        [DataMember(Name = "status", Order = 3,
            EmitDefaultValue = false)]
        public int Status { get; set; }
    }
}
