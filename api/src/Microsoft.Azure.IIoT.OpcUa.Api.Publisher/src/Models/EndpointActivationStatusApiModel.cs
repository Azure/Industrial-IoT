// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Endpoint Activation status model
    /// </summary>
    [DataContract]
    public class EndpointActivationStatusApiModel {

        /// <summary>
        /// Identifier of the endoint
        /// </summary>
        [DataMember(Name = "id", Order = 0)]
        [Required]
        public string Id { get; set; }

        /// <summary>
        /// Activation state
        /// </summary>
        [DataMember(Name = "activationState", Order = 1,
            EmitDefaultValue = false)]
        public EndpointActivationState? ActivationState { get; set; }
    }
}
