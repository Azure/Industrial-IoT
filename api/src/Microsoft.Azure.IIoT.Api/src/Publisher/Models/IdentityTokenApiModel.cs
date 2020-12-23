// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System.Runtime.Serialization;
    using System;

    /// <summary>
    /// Identity token
    /// </summary>
    [DataContract]
    public class IdentityTokenApiModel {

        /// <summary>
        /// Identity
        /// </summary>
        [DataMember(Name = "identity", Order = 0)]
        public string Identity { get; set; }

        /// <summary>
        /// Key
        /// </summary>
        [DataMember(Name = "key", Order = 1)]
        public string Key { get; set; }

        /// <summary>
        /// Expiration
        /// </summary>
        [DataMember(Name = "expires", Order = 2)]
        public DateTime Expires { get; set; }
    }
}