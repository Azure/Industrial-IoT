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
        [DataMember(Name = "method")]
        public string Method { get; set; }

        /// <summary>
        /// Resource id
        /// </summary>
        [DataMember(Name = "resourceId",
            EmitDefaultValue = false)]
        public string ResourceId { get; internal set; }

        /// <summary>
        /// Uri to call
        /// </summary>
        [DataMember(Name = "uri")]
        public string Uri { get; internal set; }

        /// <summary>
        /// Headers
        /// </summary>
        [DataMember(Name = "requestHeaders",
            EmitDefaultValue = false)]
        public Dictionary<string, List<string>> RequestHeaders { get; set; }

        /// <summary>
        /// Headers
        /// </summary>
        [DataMember(Name = "contentHeaders",
            EmitDefaultValue = false)]
        public Dictionary<string, List<string>> ContentHeaders { get; set; }
    }
}
