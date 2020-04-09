// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Events.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;

    /// <summary>
    /// Supervisor event
    /// </summary>
    public class SupervisorEventModel {

        /// <summary>
        /// Event type
        /// </summary>
        public SupervisorEventType EventType { get; set; }

        /// <summary>
        /// Context
        /// </summary>
        public RegistryOperationContextModel Context { get; set; }

        /// <summary>
        /// Supervisor id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Supervisor
        /// </summary>
        public SupervisorModel Supervisor { get; set; }
    }
}