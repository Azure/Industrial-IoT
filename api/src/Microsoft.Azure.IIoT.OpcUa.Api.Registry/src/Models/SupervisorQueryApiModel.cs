// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Supervisor registration query
    /// </summary>
    [DataContract]
    public class SupervisorQueryApiModel {

        /// <summary>
        /// Site for the supervisors
        /// </summary>
        [DataMember(Name = "siteId", Order = 0,
            EmitDefaultValue = false)]
        public string SiteId { get; set; }

        /// <summary>
        /// Managing provided endpoint twin
        /// </summary>
        [DataMember(Name = "endpointId", Order = 1,
            EmitDefaultValue = false)]
        public string EndpointId { get; set; }

        /// <summary>
        /// Included connected or disconnected
        /// </summary>
        [DataMember(Name = "connected", Order = 2,
            EmitDefaultValue = false)]
        public bool? Connected { get; set; }
    }
}
