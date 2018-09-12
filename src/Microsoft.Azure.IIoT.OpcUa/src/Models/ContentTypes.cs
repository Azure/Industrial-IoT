// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Models {

    /// <summary>
    /// Content type constants
    /// </summary>
    public static class ContentTypes {

        /// <summary>
        /// Message contains discover requests
        /// </summary>
        public const string DiscoveryRequest =
            "application/x-discovery-request-v1-json";

        /// <summary>
        /// Message contains discovery events
        /// </summary>
        public const string DiscoveryEvent =
            "application/x-discovery-event-v1-json";

        /// <summary>
        /// Message contains publish event
        /// </summary>
        public const string PublishEvent =
            "application/x-publish-event-v1-json";
    }
}
