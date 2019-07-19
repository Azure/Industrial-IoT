// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.CosmosDB.BulkExecutor.Graph {
    using Microsoft.Azure.CosmosDB.BulkExecutor.Graph.Element;
    using Microsoft.Azure.IIoT.Storage;
    using Microsoft.Azure.IIoT.Storage.Annotations;
    using Gremlin.Net.CosmosDb.Structure;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Concurrent;
    using System.Reflection;
    using Newtonsoft.Json;

    /// <summary>
    /// Gremlin edge extensions
    /// </summary>
    public static class GremlinElementEx {

        /// <summary>
        /// Convert to gremlin edge
        /// </summary>
        /// <param name="edge"></param>
        /// <param name="serializer"></param>
        /// <param name="outV"></param>
        /// <param name="inV"></param>
        /// <returns></returns>
        public static GremlinEdge ToEdge<V1, E, V2>(this E edge, JsonSerializer serializer,
            V1 outV, V2 inV) {
            outV.ToJElement(serializer, out var outId, out var outLabel, out var outPk);
            inV.ToJElement(serializer, out var inId, out var inLabel, out var inPk);

            var jEdge = edge.ToJElement(serializer, out var edgeId, out var edgeLabel,
                out var edgePk);
            var gedge = new GremlinEdge(edgeId, edgeLabel, outId, inId, outLabel,
                inLabel, outPk, inPk);
            foreach (var prop in jEdge) {
                switch (prop.Key.ToLowerInvariant()) {
                    case DocumentProperties.IdKey:
                    case DocumentProperties.LabelKey:
                    case DocumentProperties.PartitionKey:
                        break;
                    default:
                        gedge.AddProperty(prop.Key, prop.Value);
                        break;
                }
            }
            return gedge;
        }

        /// <summary>
        /// Convert to gremlin vertex
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="vertex"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public static GremlinVertex ToVertex<V>(this V vertex, JsonSerializer serializer) {
            var jvertex = vertex.ToJElement(serializer, out var id, out var label, out var pk);
            var gvertex = new GremlinVertex(id, label);
            foreach (var prop in jvertex) {
                switch (prop.Key.ToLowerInvariant()) {
                    case DocumentProperties.IdKey:
                    case DocumentProperties.LabelKey:
                    case DocumentProperties.PartitionKey:
                        break;
                    default:
                        gvertex.AddProperty(prop.Key, prop.Value);
                        break;
                }
            }
            return gvertex;
        }

        /// <summary>
        /// Extracts label, partition key and identifier from the object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="element"></param>
        /// <param name="serializer"></param>
        /// <param name="id"></param>
        /// <param name="label"></param>
        /// <param name="pk"></param>
        /// <returns></returns>
        private static JObject ToJElement<T>(this T element, JsonSerializer serializer,
            out string id, out string label, out string pk) {
            var jelement = JObject.FromObject(element, serializer);

            id = jelement.GetValueOrDefault(DocumentProperties.IdKey,
                () => Guid.NewGuid().ToString());

            pk = jelement.GetValueOrDefault(DocumentProperties.PartitionKey,
                () => Guid.NewGuid().ToString());

            label = jelement.GetValueOrDefault(DocumentProperties.LabelKey,
                () => LabelCache.GetOrAdd(typeof(T), t => {
                    var tattr = typeof(T).GetCustomAttribute<TypeNameAttribute>(true);
                    if (tattr != null && tattr.Name != null) {
                        return tattr.Name;
                    }
                    var lattr = typeof(T).GetCustomAttribute<LabelAttribute>(true);
                    if (lattr != null && lattr.Name != null) {
                        return lattr.Name;
                    }
                    return typeof(T).Name.Substring(0, 1).ToLower() +
                        typeof(T).Name.Substring(1);
                }));
            return jelement;
        }

        internal static ConcurrentDictionary<Type, string> LabelCache { get; } =
            new ConcurrentDictionary<Type, string>();
    }
}
