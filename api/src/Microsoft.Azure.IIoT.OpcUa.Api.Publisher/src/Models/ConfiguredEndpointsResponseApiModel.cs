// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;


    /// <summary>
    /// GetConfiguredEndpoints direct method response
    /// </summary>
    [DataContract]
    public class ConfiguredEndpointsResponseApiModel {

        /// <summary>
        /// Configured endpoint urls
        /// </summary>
        [DataMember(Name = "endpoints", Order = 0)]
        [Required]
        public List<ConfiguredEndpointModel> Endpoints { get; set; }

    }
}
