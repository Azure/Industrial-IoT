// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas
{
    using Azure.IIoT.OpcUa.Core.Messaging;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Creates message schemas that describe the network messages a writer
    /// group publishes. The schemas are surfaced to callers as
    /// <see cref="IEventSchema"/> and registered with schema registries.
    /// </summary>
    /// <remarks>
    /// This factory is deliberately independent of the message encoders so
    /// that schema publication survives changes to the encoding runtime.
    /// </remarks>
    public static class NetworkMessageSchema
    {
        /// <summary>
        /// Try to create a network message schema for the given encoding.
        /// </summary>
        /// <param name="encoding"></param>
        /// <param name="networkMessage"></param>
        /// <param name="schema"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static bool TryCreate(MessageEncoding encoding,
            PublishedNetworkMessageSchemaModel networkMessage,
            [NotNullWhen(true)] out IEventSchema? schema, SchemaOptions? options = null)
        {
            if (encoding.HasFlag(MessageEncoding.Json))
            {
                schema = new Json.JsonNetworkMessage(networkMessage, options);
            }
            else if (encoding.HasFlag(MessageEncoding.Uadp))
            {
                schema = new Uadp.UadpNetworkMessage(networkMessage);
            }
            else
            {
                schema = default;
                return false;
            }
            return true;
        }
    }
}
