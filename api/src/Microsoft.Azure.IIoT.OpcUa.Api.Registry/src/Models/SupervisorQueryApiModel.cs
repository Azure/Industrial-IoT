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
        [DataMember(Name = "siteId",
            EmitDefaultValue = false)]
        public string SiteId { get; set; }

        /// <summary>
        /// Included connected or disconnected
        /// </summary>
        [DataMember(Name = "connected",
            EmitDefaultValue = false)]
        public bool? Connected { get; set; }
    }
}
