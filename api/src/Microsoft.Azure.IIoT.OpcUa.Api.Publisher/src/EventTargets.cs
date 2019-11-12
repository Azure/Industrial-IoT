// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher {

    /// <summary>
    /// Event target constants
    /// </summary>
    public static class EventTargets {

        /// <summary>
        /// Nodes group
        /// </summary>
        public const string Nodes = "publish";

        /// <summary>
        /// Endpoints group
        /// </summary>
        public const string Endpoints = "endpoints";

        /// <summary>
        /// Discovery progress event targets
        /// </summary>
        public const string PublisherSampleTarget = "PublisherMessage";
    }
}
