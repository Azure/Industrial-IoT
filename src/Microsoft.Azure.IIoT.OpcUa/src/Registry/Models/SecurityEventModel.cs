// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models
{
    /// <summary>
    /// Endpoint info
    /// </summary>
    public class SecurityEventModel
    {
        /// <summary>
        /// Event Type
        /// </summary>
        public string EventType { get; set; } = "Operational";

        /// <summary>
        /// Category of the event
        /// </summary>
        public string Category { get; set; } = "Triggered";

        /// <summary>
        /// Name of the event
        /// </summary>
        public string Name { get; set; } = "ConfigurationError";

        /// <summary>
        /// Whether event is empty or not
        /// </summary>
        public bool IsEmpty { get; set; } = false;

        /// <summary>
        /// Payload Schema Version
        /// </summary>
        public string PayloadSchemaVersion { get; set; } = "1.0";

        /// <summary>
        /// Id of the event
        /// </summary>
        public string Id { get; set; } = "1111db0b-44fe-42e9-9cff-bbb2d8fd0000";

        /// <summary>
        /// TimestmapLocal
        /// </summary>
        public string TimestampLocal { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ssZ");

        /// <summary>
        /// TimestampUTC
        /// </summary>
        public string TimestampUTC { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ssZ");

        /// <summary>
        /// Payload
        /// </summary>
        public List<SecurityEventPayloadModel> Payload { get; set; }

    }
}
