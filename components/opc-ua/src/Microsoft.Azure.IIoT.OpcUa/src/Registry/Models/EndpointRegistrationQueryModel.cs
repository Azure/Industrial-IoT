// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {

    /// <summary>
    /// Endpoint query
    /// </summary>
    public class EndpointRegistrationQueryModel {

        /// <summary>
        /// Endoint url for direct server access
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Certificate of the endpoint
        /// </summary>
        public byte[] Certificate { get; set; }

        /// <summary>
        /// Endpoint security policy to use - null = Best.
        /// </summary>
        public string SecurityPolicy { get; set; }

        /// <summary>
        /// Security mode to use for communication - null = Best
        /// </summary>
        public SecurityMode? SecurityMode { get; set; }

        /// <summary>
        /// Whether the endpoint is activated
        /// </summary>
        public bool? Activated { get; set; }

        /// <summary>
        /// Whether the endpoint is connected
        /// </summary>
        public bool? Connected { get; set; }

        /// <summary>
        /// The last state of the the activated endpoint
        /// </summary>
        public EndpointConnectivityState? EndpointState { get; set; }

        /// <summary>
        /// Whether to include endpoints that were soft deleted
        /// </summary>
        public bool? IncludeNotSeenSince { get; set; }
    }
}

