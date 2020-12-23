// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Core.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Application type
    /// </summary>
    [DataContract]
    public enum ApplicationType {

        /// <summary>
        /// Server
        /// </summary>
        [EnumMember]
        Server,

        /// <summary>
        /// Client
        /// </summary>
        [EnumMember]
        Client,

        /// <summary>
        /// Client and server
        /// </summary>
        [EnumMember]
        ClientAndServer,

        /// <summary>
        /// Discovery server
        /// </summary>
        [EnumMember]
        DiscoveryServer
    }
}
