// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    /// <summary>
    /// Publisher update request
    /// </summary>
    public class PublisherUpdateModel {

        /// <summary>
        /// Site of the publisher
        /// </summary>
        public string SiteId { get; set; }

        /// <summary>
        /// Publisher configuration
        /// </summary>
        public PublisherConfigModel Configuration { get; set; }

        /// <summary>
        /// Current log level
        /// </summary>
        public TraceLogLevel? LogLevel { get; set; }
    }
}
