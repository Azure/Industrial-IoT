// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;

    /// <summary>
    /// Specialized services provided by endpoint identity
    /// </summary>
    public interface ITwinServices {

        /// <summary>
        /// Current state to report
        /// </summary>
        public EndpointConnectivityState State { get; }
    }
}
