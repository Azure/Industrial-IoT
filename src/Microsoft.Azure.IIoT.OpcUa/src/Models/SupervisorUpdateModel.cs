// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Models {
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Supervisor registration update request
    /// </summary>
    public class SupervisorUpdateModel {

        /// <summary>
        /// Identifier of the supervisor
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Site of the supervisor
        /// </summary>
        public string SiteId { get; set; }

        /// <summary>
        /// Supervisor discovery mode
        /// </summary>
        public DiscoveryMode? Discovery { get; set; }

        /// <summary>
        /// Supervisor discovery config
        /// </summary>
        public DiscoveryConfigModel DiscoveryConfig { get; set; }

        /// <summary>
        /// Callbacks to add or remove (see below)
        /// </summary>
        public List<CallbackModel> DiscoveryCallbacks { get; set; }

        /// <summary>
        /// Whether to add or remove callbacks
        /// </summary>
        public bool? RemoveDiscoveryCallbacks { get; set; }

        /// <summary>
        /// Supervisor client cert
        /// </summary>
        public byte[] Certificate { get; set; }
    }
}
