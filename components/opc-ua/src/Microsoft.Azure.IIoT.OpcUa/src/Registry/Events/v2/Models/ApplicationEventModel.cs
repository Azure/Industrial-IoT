// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Events.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;

    /// <summary>
    /// Application event
    /// </summary>
    public class ApplicationEventModel {

        /// <summary>
        /// Event type
        /// </summary>
        public ApplicationEventType EventType { get; set; }

        /// <summary>
        /// Context
        /// </summary>
        public RegistryOperationContextModel Context { get; set; }

        /// <summary>
        /// Application id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Application
        /// </summary>
        public ApplicationInfoModel Application { get; set; }
    }
}