// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.EventHubs
{
    /// <summary>
    /// Azure Schema Registry options.
    /// </summary>
    public sealed class SchemaRegistryOptions
    {
        /// <summary>
        /// Fully qualified namespace.
        /// </summary>
        public required string FullyQualifiedNamespace { get; set; }

        /// <summary>
        /// Schema group name.
        /// </summary>
        public required string SchemaGroupName { get; set; }
    }
}
