// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Subscriber.Models {

    /// <summary>
    /// Content type constants
    /// </summary>
    public static class ContentTypes {

        /// <summary>
        /// Message contains a single publisher sample
        /// </summary>
        public const string SubscriberSample =
            "application/x-publisher-sample-v2-json";

        /// <summary>
        /// message contains a single legacy publisher message
        /// </summary>
        public const string LegacySubscriberSample =
            "application/opcua+uajson";
    }
}
