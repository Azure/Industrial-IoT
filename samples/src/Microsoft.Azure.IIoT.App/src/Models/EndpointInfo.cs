// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;

    /// <summary>
    /// Endpoint info wrapper
    /// </summary>
    public class EndpointInfo {

        /// <summary>
        /// Model
        /// </summary>
        public EndpointInfoApiModel EndpointModel { get; set; }

        /// <summary>
        /// State
        /// </summary>
        public bool EndpointState { get; set; }
    }
}
