// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using System.Runtime.Serialization;
    using System.ComponentModel.DataAnnotations;
    using System;

    /// <summary>
    /// Registry operation log model
    /// </summary>
    [DataContract]
    public class RegistryOperationApiModel {

        /// <summary>
        /// Operation User
        /// </summary>
        [DataMember(Name = "authorityId", Order = 0)]
        [Required]
        public string AuthorityId { get; set; }

        /// <summary>
        /// Operation time
        /// </summary>
        [DataMember(Name = "time", Order = 1)]
        [Required]
        public DateTime Time { get; set; }
    }
}

