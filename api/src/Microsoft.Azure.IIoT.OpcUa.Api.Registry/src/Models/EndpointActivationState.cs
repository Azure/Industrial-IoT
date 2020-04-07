// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Activation state of the endpoint twin
    /// </summary>
    [DataContract]
    public enum EndpointActivationState {

        /// <summary>
        /// Endpoint twin is deactivated (default)
        /// </summary>
        [EnumMember]
        Deactivated,

        /// <summary>
        /// Endpoint twin is activated but not connected
        /// </summary>
        [EnumMember]
        Activated,

        /// <summary>
        /// Endoint twin is activated and connected to hub
        /// </summary>
        [EnumMember]
        ActivatedAndConnected
    }
}
