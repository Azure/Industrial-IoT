// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Api.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Gateway modules
    /// </summary>
    [DataContract]
    public record class GatewayModulesModel {

        /// <summary>
        /// Publisher identity if deployed
        /// </summary>
        [DataMember(Name = "publisher", Order = 1,
            EmitDefaultValue = false)]
        public PublisherModel Publisher { get; set; }
   }
}
