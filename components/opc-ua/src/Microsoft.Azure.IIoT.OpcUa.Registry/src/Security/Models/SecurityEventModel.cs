// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Security.Models {
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Endpoint info
    /// </summary>
    public class SecurityEventModel {

        /// <summary>
        /// Event Type
        /// </summary>
        public string EventType { get; set; }

        /// <summary>
        /// Category of the event
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Name of the event
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Whether event is empty or not
        /// </summary>
        public bool IsEmpty { get; set; }

        /// <summary>
        /// Payload Schema Version
        /// </summary>
        public string PayloadSchemaVersion { get; set; }

        /// <summary>
        /// Id of the event
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// TimestmapLocal
        /// </summary>
        public string TimestampLocal { get; set; }

        /// <summary>
        /// TimestampUTC
        /// </summary>
        public string TimestampUTC { get; set; }

        /// <summary>
        /// Payload
        /// </summary>
        public List<SecurityEventPayloadModel> Payload { get; set; }
    }
}
