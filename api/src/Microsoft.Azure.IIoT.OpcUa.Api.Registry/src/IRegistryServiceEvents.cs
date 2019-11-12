// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry {
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using Microsoft.Azure.IIoT.Messaging;

    /// <summary>
    /// Registry service events
    /// </summary>
    public interface IRegistryServiceEvents {

        /// <summary>
        /// Application events
        /// </summary>
        IEventSource<ApplicationEventApiModel> Applications { get; }

        /// <summary>
        /// Endpoint events
        /// </summary>
        IEventSource<EndpointEventApiModel> Endpoints { get; }

        /// <summary>
        /// Supervisor events
        /// </summary>
        IEventSource<SupervisorEventApiModel> Supervisors { get; }

        /// <summary>
        /// Publisher events
        /// </summary>
        IEventSource<PublisherEventApiModel> Publishers { get; }

        /// <summary>
        /// Discovery events
        /// </summary>
        IEventSource<DiscoveryProgressApiModel> Supervisor(
            string supervisorId);

        /// <summary>
        /// Discovery events
        /// </summary>
        IEventSource<DiscoveryProgressApiModel> Discovery(
            string requestId);
    }
}
