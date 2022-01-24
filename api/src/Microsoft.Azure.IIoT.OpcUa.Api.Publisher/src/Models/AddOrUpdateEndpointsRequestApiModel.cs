// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {

    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// DM API model that contains list of updates to endpoints.
    /// </summary>
    [DataContract]
    public class AddOrUpdateEndpointsRequestApiModel {
        /// <summary> Definitions of the endpoints that should be updated. </summary>
        [DataMember(Name = "endpoints", Order = 0)]
        [Required]
        public List<PublishNodesRequestApiModel> Endpoints { get; set; }
    }
}
