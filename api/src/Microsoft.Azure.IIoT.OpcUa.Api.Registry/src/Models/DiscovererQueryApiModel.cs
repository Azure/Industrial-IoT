// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Discoverer registration query
    /// </summary>
    [DataContract]
    public class DiscovererQueryApiModel {

        /// <summary>
        /// Site of the discoverer
        /// </summary>
        [DataMember(Name = "siteId",
            EmitDefaultValue = false)]
        public string SiteId { get; set; }

        /// <summary>
        /// Discovery mode of discoverer
        /// </summary>
        [DataMember(Name = "discovery",
            EmitDefaultValue = false)]
        public DiscoveryMode? Discovery { get; set; }

        /// <summary>
        /// Included connected or disconnected
        /// </summary>
        [DataMember(Name = "connected",
            EmitDefaultValue = false)]
        public bool? Connected { get; set; }
    }
}
