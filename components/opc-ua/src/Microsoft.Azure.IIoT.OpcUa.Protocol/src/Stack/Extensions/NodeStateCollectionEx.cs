// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Extensions {
    using System;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Xml;
    using Opc.Ua.Encoders;
    using Opc.Ua.Nodeset.Schema;
    using Opc.Ua.Nodeset;

    /// <summary>
    /// Node state extensions
    /// </summary>
    public static class NodeStateCollectionEx {

        /// <summary>
        /// Writes the collection to a stream using the NodeSet schema.
        /// </summary>
        public static void SaveAsNodeSet(this NodeStateCollection collection, Stream ostrm,
            ISystemContext context) {
            var nodeTable = new NodeTable(context.NamespaceUris, context.ServerUris, null);
            foreach (var node in collection) {
                node.Export(context, nodeTable);
            }
            var nodeSet = new NodeSet();
            foreach (ILocalNode node in nodeTable) {
                nodeSet.Add(node, nodeTable.NamespaceUris, nodeTable.ServerUris);
            }
            var settings = new XmlWriterSettings {
                Encoding = Encoding.UTF8,
                CloseOutput = true,
                ConformanceLevel = ConformanceLevel.Document,
                Indent = true
            };
            using (var writer = XmlWriter.Create(ostrm, settings)) {
                var serializer = new DataContractSerializer(typeof(NodeSet));
                serializer.WriteObject(writer, nodeSet);
            }
        }

        /// <summary>
        /// Writes collection as json
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="ostrm"></param>
        /// <param name="formatting"></param>
        /// <param name="context"></param>
        public static void SaveAsJson(this NodeStateCollection collection, Stream ostrm,
            Newtonsoft.Json.Formatting formatting, ISystemContext context) {
            using (var encoder = new JsonEncoderEx(ostrm, context.ToMessageContext(),
                        JsonEncoderEx.JsonEncoding.Array, formatting) {
                UseAdvancedEncoding = true,
                IgnoreDefaultValues = true
            }) {
                foreach (var node in collection.ToNodeModels(context)) {
                    if (node != null) {
                        encoder.WriteEncodeable(null, new EncodeableNodeModel(node));
                    }
                }
            }
        }

        /// <summary>
        /// Writes the collection to a stream using the Opc.Ua.Schema.UANodeSet schema.
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="ostrm"></param>
        /// <param name="lastModified"></param>
        /// <param name="context"></param>
        /// <param name="model"></param>
        public static void SaveAsNodeSet2(this NodeStateCollection collection, Stream ostrm,
            DateTime? lastModified, ISystemContext context, ModelTableEntry model = null) {
            NodeSet2.Create(collection.ToNodeModels(context), model, lastModified, context).Save(ostrm);
        }
    }
}
