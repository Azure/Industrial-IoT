// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models {
    using System;

    /// <summary>
    /// Network message model
    /// </summary>
    public class NetworkMessageModel {

        /// <summary>
        /// Processing timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Content type
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Content encoding
        /// </summary>
        public string ContentEncoding { get; set; }

        /// <summary>
        /// Message schema
        /// </summary>
        public string MessageSchema { get; set; }

        /// <summary>
        /// Custom routing info to be added to the header.
        /// </summary>
        public string RoutingInfo { get; set; }

        /// <summary>
        /// Message body
        /// </summary>
        public byte[] Body { get; set; }
    }
}
