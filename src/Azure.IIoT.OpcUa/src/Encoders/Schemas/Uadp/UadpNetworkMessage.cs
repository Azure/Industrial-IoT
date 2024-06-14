// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas.Uadp
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly;
    using Furly.Extensions.Messaging;
    using System;
    using System.Linq;
    using System.Text.Json;

    /// <summary>
    /// Network message schema for uadp. Enables a consumer
    /// to decode a network message using a resolver
    /// </summary>
    public sealed class UadpNetworkMessage : IEventSchema
    {
        /// <inheritdoc/>
        public string Type => ContentMimeType.Json;

        /// <inheritdoc/>
        public string Name { get; }

        /// <inheritdoc/>
        public ulong Version { get; }

        /// <inheritdoc/>
        public string? Id { get; }

        /// <inheritdoc/>
        public string Schema { get; }

        /// <summary>
        /// Get avro schema for a writer group
        /// </summary>
        /// <param name="networkMessage"></param>
        public UadpNetworkMessage(PublishedNetworkMessageSchemaModel networkMessage)
        {
            ArgumentNullException.ThrowIfNull(networkMessage);

            Schema = JsonSerializer.Serialize(networkMessage);
            var minor = networkMessage.DataSetMessages?
                .Max(dataSet => dataSet?.MetaData?.MinorVersion ?? 0) ?? 0;
            var major = networkMessage.DataSetMessages?
                .Max(dataSet => dataSet?.MetaData?.DataSetMetaData.MajorVersion ?? 0) ?? 0;
            Version = ((ulong)major << 32) + minor;
            Name = networkMessage.TypeName ?? string.Empty;
        }

        /// <inheritdoc/>
        public override string? ToString()
        {
            return Schema;
        }
    }
}
