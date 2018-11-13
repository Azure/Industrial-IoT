// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Twin {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// All twin related services
    /// </summary>
    public class TwinServices : ITwinServices {

        /// <inheritdoc/>
        public EndpointModel Endpoint { get; set; }

        /// <inheritdoc/>
        public Task SetEndpointAsync(EndpointModel endpoint) {
            Endpoint = endpoint;
            return Task.CompletedTask;
        }
    }
}
