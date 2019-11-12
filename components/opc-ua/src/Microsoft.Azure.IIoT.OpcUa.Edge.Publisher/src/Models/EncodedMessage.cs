// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models {
    using System;

    /// <summary>
    /// Encoded message model
    /// </summary>
    public class EncodedMessage {

        /// <summary>
        /// Message identifier
        /// </summary>
        public string MessageId { get; set; }

        /// <summary>
        /// Processing timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Message body
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// Content type
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Message schema
        /// </summary>
        public string MessageSchema { get; set; }
    }
}