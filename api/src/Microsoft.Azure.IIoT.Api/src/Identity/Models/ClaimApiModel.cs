// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Identity.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Instance of Claim.
    /// </summary>
    [DataContract]
    public class ClaimApiModel {

        /// <summary>
        /// Claim Type.
        /// </summary>
        [DataMember]
        public string Type { get; set; }

        /// <summary>
        /// Claim Value.
        /// </summary>
        [DataMember]
        public string Value { get; set; }
    }
}