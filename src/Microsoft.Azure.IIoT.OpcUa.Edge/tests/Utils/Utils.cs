// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Control {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
    using Newtonsoft.Json.Linq;
    using System.Threading.Tasks;
    using Opc.Ua.Extensions;
    using Opc.Ua;

    public static class EndpointServicesEx {

        /// <summary>
        /// Read value
        /// </summary>
        /// <param name="client"></param>
        /// <param name="endpoint"></param>
        /// <param name="readNode"></param>
        /// <returns></returns>
        public static Task<JToken> ReadValueAsync(this IEndpointServices client,
            EndpointModel endpoint, string readNode) {
            var codec = new JsonVariantEncoder();
            return client.ExecuteServiceAsync(endpoint, session => {
                var nodesToRead = new ReadValueIdCollection {
                    new ReadValueId {
                        NodeId = readNode.ToNodeId(session.MessageContext),
                        AttributeId = Attributes.Value
                    }
                };
                var responseHeader = session.Read(null, 0, TimestampsToReturn.Both,
                    nodesToRead, out var values, out var diagnosticInfos);
                var result = codec.Encode(values[0].WrappedValue,
                    out var tmp, session.MessageContext);
                return Task.FromResult(result);
            });
        }

    }
}
