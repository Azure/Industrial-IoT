// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Azure.IIoT.OpcUa.Encoders;
    using Furly.Extensions.Serializers;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Session provider extensions
    /// </summary>
    public static class OpcUaClientManagerEx
    {
        /// <summary>
        /// Read value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="client"></param>
        /// <param name="connection"></param>
        /// <param name="readNode"></param>
        /// <param name="serializer"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static Task<VariantValue> ReadValueAsync<T>(this IOpcUaClientManager<T> client,
            T connection, string readNode, IJsonSerializer serializer, CancellationToken ct = default)
        {
            return client.ExecuteAsync(connection, async context =>
            {
                var nodesToRead = new ReadValueIdCollection
                {
                    new ReadValueId
                    {
                        NodeId = readNode.ToNodeId(context.Session.MessageContext),
                        AttributeId = Attributes.Value
                    }
                };
                var response = await context.Session.Services.ReadAsync(
                    new RequestHeader(), 0, TimestampsToReturn.Both,
                    nodesToRead, context.Ct).ConfigureAwait(false);
                return new JsonVariantEncoder(context.Session.MessageContext, serializer)
                    .Encode(response.Results[0].WrappedValue, out var tmp);
            }, ct: ct);
        }
    }
}
