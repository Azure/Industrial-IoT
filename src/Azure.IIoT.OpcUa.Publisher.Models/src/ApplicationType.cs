// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Application type
    /// </summary>
    [DataContract]
    public enum ApplicationType
    {
        /// <summary>
        /// Application is server
        /// </summary>
        [EnumMember(Value = "Server")]
        Server,

        /// <summary>
        /// Application is client
        /// </summary>
        [EnumMember(Value = "Client")]
        Client,

        /// <summary>
        /// Application is client and server
        /// </summary>
        [EnumMember(Value = "ClientAndServer")]
        ClientAndServer,

        /// <summary>
        /// Application is discovery server
        /// </summary>
        [EnumMember(Value = "DiscoveryServer")]
        DiscoveryServer
    }
}
