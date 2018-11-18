// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using System;

    /// <summary>
    /// Endpoint info
    /// </summary>
    public class EndpointInfoModel {

        /// <summary>
        /// Endpoint registration
        /// </summary>
        public EndpointRegistrationModel Registration { get; set; }

        /// <summary>
        /// Application id endpoint is registered under.
        /// </summary>
        public string ApplicationId { get; set; }

        /// <summary>
        /// Last time application was seen
        /// </summary>
        public DateTime? NotSeenSince { get; set; }

        /// <summary>
        /// Whether endpoint is activated in the twin module
        /// </summary>
        public bool? Activated { get; set; }

        /// <summary>
        /// Whether endpoint is connected through the twin module
        /// </summary>
        public bool? Connected { get; set; }

        /// <summary>
        /// Whether the registration is out of sync between
        /// client (module) and server (service) (default: false).
        /// </summary>
        public bool? OutOfSync { get; set; }
    }
}
