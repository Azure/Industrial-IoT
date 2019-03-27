// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {

    /// <summary>
    /// Content type constants
    /// </summary>
    public static class ContentTypes {

        /// <summary>
        /// Message contains discover requests
        /// </summary>
        public const string DiscoveryRequest =
            "application/x-discovery-request-v2-json";

        /// <summary>
        /// Message contains discovery events
        /// </summary>
        public const string DiscoveryEvent =
            "application/x-discovery-event-v2-json";

        /// <summary>
        /// Content is a nodeset
        /// </summary>
        public const string NodeSet =
            "application/x-node-set-v1";
    }
}
