// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Onboarding.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Discovery mode to use
    /// </summary>
    [DataContract]
    public enum DiscoveryMode {

        /// <summary>
        /// No discovery
        /// </summary>
        [EnumMember]
        Off,

        /// <summary>
        /// Find and use local discovery server on edge device
        /// </summary>
        [EnumMember]
        Local,

        /// <summary>
        /// Find and use all LDS in all connected networks
        /// </summary>
        [EnumMember]
        Network,

        /// <summary>
        /// Fast network scan of */24 and known list of ports
        /// </summary>
        [EnumMember]
        Fast,

        /// <summary>
        /// Perform a deep scan of all networks.
        /// </summary>
        [EnumMember]
        Scan
    }
}
