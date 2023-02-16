// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Api.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Result of an application registration
    /// </summary>
    [DataContract]
    public record class ApplicationRegistrationResponseModel {

        /// <summary>
        /// New id application was registered under
        /// </summary>
        [DataMember(Name = "id", Order = 0)]
        public string Id { get; set; }
    }
}
