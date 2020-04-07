// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Signing request response
    /// </summary>
    [DataContract]
    public sealed class StartSigningRequestResponseApiModel {

        /// <summary>
        /// Request id
        /// </summary>
        [DataMember(Name = "requestId", Order = 0)]
        public string RequestId { get; set; }
    }
}
