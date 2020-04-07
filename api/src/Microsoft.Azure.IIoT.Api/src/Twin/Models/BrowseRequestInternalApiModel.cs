// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Browse request model for module
    /// </summary>
    [DataContract]
    public class BrowseRequestInternalApiModel : BrowseRequestApiModel {

        /// <summary>
        /// Whether to only return the raw node id
        /// information and not read the target node.
        /// (default is false)
        /// </summary>
        [DataMember(Name = "NodeIdsOnly", Order = 10,
            EmitDefaultValue = false)]
        public bool? NodeIdsOnly { get; set; }
    }
}
