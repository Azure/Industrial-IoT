// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {

    /// <summary>
    /// Publisher registration
    /// </summary>
    public class PublisherModel {

        /// <summary>
        /// Identifier of the publisher
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Site of the publisher
        /// </summary>
        public string SiteId { get; set; }

        /// <summary>
        /// Current log level
        /// </summary>
        public TraceLogLevel? LogLevel { get; set; }

        /// <summary>
        /// Publisher configuration
        /// </summary>
        public PublisherConfigModel Configuration { get; set; }

        /// <summary>
        /// Whether the registration is out of sync between
        /// client (module) and server (service) (default: false).
        /// </summary>
        public bool? OutOfSync { get; set; }

        /// <summary>
        /// Whether publisher is connected
        /// </summary>
        public bool? Connected { get; set; }
    }
}
