// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.EventHubs
{
    /// <summary>
    /// Event Hubs client options.
    /// </summary>
    public sealed class EventHubsClientOptions
    {
        /// <summary>
        /// Connection string.
        /// </summary>
        public string? ConnectionString { get; set; }

        /// <summary>
        /// Maximum payload size. Default is 1 MB.
        /// </summary>
        public int? MaxEventPayloadSizeInBytes { get; set; }

        /// <summary>
        /// Schema registry options. Set to null to disable publishing schemas.
        /// </summary>
        public SchemaRegistryOptions? SchemaRegistry { get; set; }
    }
}
