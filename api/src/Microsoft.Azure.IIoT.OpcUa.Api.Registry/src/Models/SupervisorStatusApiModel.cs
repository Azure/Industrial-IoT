// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using System.Runtime.Serialization;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Supervisor runtime status
    /// </summary>
    [DataContract]
    public class SupervisorStatusApiModel {

        /// <summary>
        /// Edge device id
        /// </summary>
        [DataMember(Name = "deviceId", Order = 0)]
        [Required]
        public string DeviceId { get; set; }

        /// <summary>
        /// Module id
        /// </summary>
        [DataMember(Name = "moduleId", Order = 1,
            EmitDefaultValue = false)]
        public string ModuleId { get; set; }

        /// <summary>
        /// Site id
        /// </summary>
        [DataMember(Name = "siteId", Order = 2,
            EmitDefaultValue = false)]
        public string SiteId { get; set; }

        /// <summary>
        /// Endpoint activation status
        /// </summary>
        [DataMember(Name = "endpoints", Order = 3,
            EmitDefaultValue = false)]
        public List<EndpointActivationStatusApiModel> Endpoints { get; set; }
    }
}
