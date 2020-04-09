// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// Crl chain model
    /// </summary>
    [DataContract]
    public sealed class X509CrlChainApiModel {

        /// <summary>
        /// Chain
        /// </summary>
        [DataMember(Name = "chain", Order = 0,
            EmitDefaultValue = false)]
        public List<X509CrlApiModel> Chain { get; set; }
    }
}
