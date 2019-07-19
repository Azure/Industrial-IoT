// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Activation state of the endpoint twin
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum EndpointActivationState {

        /// <summary>
        /// Endpoint twin is deactivated
        /// </summary>
        Deactivated,

        /// <summary>
        /// Endpoint twin is activated but not connected
        /// </summary>
        Activated,

        /// <summary>
        /// Endoint twin is activated and connected to hub
        /// </summary>
        ActivatedAndConnected
    }
}
