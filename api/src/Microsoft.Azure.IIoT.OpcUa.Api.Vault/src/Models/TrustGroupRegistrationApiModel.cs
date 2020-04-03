// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Trust group registration model
    /// </summary>
    [DataContract]
    public sealed class TrustGroupRegistrationApiModel {

        /// <summary>
        /// The registered id of the trust group
        /// </summary>
        [DataMember(Name = "id", Order = 0)]
        public string Id { get; set; }

        /// <summary>
        /// Trust group
        /// </summary>
        [DataMember(Name = "group", Order = 1)]
        public TrustGroupApiModel Group { get; set; }
    }
}
