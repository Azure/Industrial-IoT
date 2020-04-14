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
    public class HttpTunnelRequestModel {

        /// <summary>
        /// Message contains request
        /// </summary>
        public const string SchemaName =
            "application/x-http-tunnel-request-v1";

        /// <summary>
        /// Method
        /// </summary>
        [DataMember(Name = "method", Order = 0)]
        public string Method { get; set; }

        /// <summary>
        /// Resource id
        /// </summary>
        [DataMember(Name = "resourceId", Order = 1,
            EmitDefaultValue = false)]
        public string ResourceId { get; internal set; }

        /// <summary>
        /// Uri to call
        /// </summary>
        [DataMember(Name = "uri", Order = 2)]
        public string Uri { get; internal set; }

        /// <summary>
        /// Headers
        /// </summary>
        [DataMember(Name = "requestHeaders", Order = 3,
            EmitDefaultValue = false)]
        public Dictionary<string, List<string>> RequestHeaders { get; set; }

        /// <summary>
        /// Headers
        /// </summary>
        [DataMember(Name = "contentHeaders", Order = 4,
            EmitDefaultValue = false)]
        public Dictionary<string, List<string>> ContentHeaders { get; set; }
    }
}
