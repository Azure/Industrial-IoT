// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Onboarding.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Application type
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ApplicationType {

        /// <summary>
        /// Server
        /// </summary>
        Server,

        /// <summary>
        /// Client
        /// </summary>
        Client,

        /// <summary>
        /// Client and server
        /// </summary>
        ClientAndServer,

        /// <summary>
        /// Discovery server
        /// </summary>
        DiscoveryServer
    }
}
