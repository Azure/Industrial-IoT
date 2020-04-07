// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// Trust group registration collection model
    /// </summary>
    [DataContract]
    public sealed class TrustGroupRegistrationListApiModel {

        /// <summary>
        /// Group registrations
        /// </summary>
        [DataMember(Name = "registrations", Order = 0)]
        public List<TrustGroupRegistrationApiModel> Registrations { get; set; }

        /// <summary>
        /// Next link
        /// </summary>
        [DataMember(Name = "nextPageLink", Order = 1,
            EmitDefaultValue = false)]
        public string NextPageLink { get; set; }
    }
}
