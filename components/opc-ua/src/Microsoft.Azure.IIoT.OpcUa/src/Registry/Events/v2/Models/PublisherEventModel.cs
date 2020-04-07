// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Events.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;

    /// <summary>
    /// Publisher event
    /// </summary>
    public class PublisherEventModel {

        /// <summary>
        /// Event type
        /// </summary>
        public PublisherEventType EventType { get; set; }

        /// <summary>
        /// Context
        /// </summary>
        public RegistryOperationContextModel Context { get; set; }

        /// <summary>
        /// Publisher id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Publisher
        /// </summary>
        public PublisherModel Publisher { get; set; }
    }
}