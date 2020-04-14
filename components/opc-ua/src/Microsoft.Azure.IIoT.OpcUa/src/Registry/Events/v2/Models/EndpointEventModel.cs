// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Events.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;

    /// <summary>
    /// Endpoint Event model for event bus
    /// </summary>
    public class EndpointEventModel {

        /// <summary>
        /// Type of event
        /// </summary>
        public EndpointEventType EventType { get; set; }

        /// <summary>
        /// Context
        /// </summary>
        public RegistryOperationContextModel Context { get; set; }

        /// <summary>
        /// Endpoint id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Endpoint info
        /// </summary>
        public EndpointInfoModel Endpoint { get; set; }
    }
}