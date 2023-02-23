// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack {
    using Azure.IIoT.OpcUa.Encoders;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Session provider extensions
    /// </summary>
    public static class SessionProviderEx {
        /// <summary>
        /// Read value
        /// </summary>
        /// <param name="client"></param>
        /// <param name="connection"></param>
        /// <param name="readNode"></param>
        /// <returns></returns>
        public static Task<VariantValue> ReadValueAsync<T>(this ISessionProvider<T> client,
            T connection, string readNode, IJsonSerializer serializer, CancellationToken ct = default) {
            return client.ExecuteServiceAsync(connection, session => {
                var nodesToRead = new ReadValueIdCollection {
                    new ReadValueId {
                        NodeId = readNode.ToNodeId(session.MessageContext),
                        AttributeId = Attributes.Value
                    }
                };
                var responseHeader = session.Session.Read(null, 0, TimestampsToReturn.Both,
                    nodesToRead, out var values, out var diagnosticInfos);
                var result = new JsonVariantEncoder(session.MessageContext, serializer)
                    .Encode(values[0].WrappedValue, out var tmp);
                return Task.FromResult(result);
            }, ct);
        }
    }
}
