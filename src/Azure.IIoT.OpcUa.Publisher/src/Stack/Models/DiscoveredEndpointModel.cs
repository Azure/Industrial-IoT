// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Models
{
    using Opc.Ua;
    using System.Collections.Generic;

    /// <summary>
    /// Endpoint information returned from discover
    /// </summary>
    public sealed record class DiscoveredEndpointModel
    {
        /// <summary>
        /// Endpoint
        /// </summary>
        public required EndpointDescription Description { get; init; }

        /// <summary>
        /// Endpoint url that can be accessed
        /// </summary>
        public required string AccessibleEndpointUrl { get; init; }

        /// <summary>
        /// Capabilities of endpoint (server)
        /// </summary>
        public required HashSet<string> Capabilities { get; init; }
    }
}
