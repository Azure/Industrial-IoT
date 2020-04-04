// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using System.Runtime.Serialization;
    using System;

    /// <summary>
    /// Registry operation log model
    /// </summary>
    [DataContract]
    public class RegistryOperationContextApiModel {

        /// <summary>
        /// User
        /// </summary>
        [DataMember(Name = "AuthorityId", Order = 0,
            EmitDefaultValue = false)]
        public string AuthorityId { get; set; }

        /// <summary>
        /// Operation time
        /// </summary>
        [DataMember(Name = "Time", Order = 1,
            EmitDefaultValue = false)]
        public DateTime Time { get; set; }
    }
}

