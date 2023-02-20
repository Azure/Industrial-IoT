// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Shared.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Application type
    /// </summary>
    [DataContract]
    public enum ApplicationType {

        /// <summary>
        /// Application is server
        /// </summary>
        [EnumMember]
        Server,

        /// <summary>
        /// Application is client
        /// </summary>
        [EnumMember]
        Client,

        /// <summary>
        /// Application is client and server
        /// </summary>
        [EnumMember]
        ClientAndServer,

        /// <summary>
        /// Application is discovery server
        /// </summary>
        [EnumMember]
        DiscoveryServer
    }
}
