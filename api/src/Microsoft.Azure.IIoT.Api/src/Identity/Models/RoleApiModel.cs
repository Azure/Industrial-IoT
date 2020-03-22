// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Identity.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Role model
    /// </summary>
    [DataContract]
    public class RoleApiModel {

        /// <summary>
        /// Role id
        /// </summary>
        [DataMember(Name = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Name
        /// </summary>
        [DataMember(Name = "id")]
        public string Name { get; set; }
    }
}
