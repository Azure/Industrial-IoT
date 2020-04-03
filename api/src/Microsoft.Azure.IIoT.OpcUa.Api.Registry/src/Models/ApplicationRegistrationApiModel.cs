// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using System.Runtime.Serialization;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Application with optional list of endpoints
    /// </summary>
    [DataContract]
    public class ApplicationRegistrationApiModel {

        /// <summary>
        /// Server information
        /// </summary>
        [DataMember(Name = "application", Order = 0)]
        [Required]
        public ApplicationInfoApiModel Application { get; set; }

        /// <summary>
        /// List of endpoint twins
        /// </summary>
        [DataMember(Name = "endpoints", Order = 1,
            EmitDefaultValue = false)]
        public List<EndpointRegistrationApiModel> Endpoints { get; set; }
    }
}
