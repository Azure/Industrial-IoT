// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
    using Opc.Ua.Models;
    using Opc.Ua.Client;
    using Opc.Ua;
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Threading;

    /// <summary>
    /// Represents a in memory node for remote reading and writing.
    /// </summary>
    public sealed class RawNodeModel : NodeAttributeSet {

        /// <summary>
        /// Constructor
        /// </summary>
        internal RawNodeModel(ExpandedNodeId nodeId, NamespaceTable namespaces) :
            base(nodeId, namespaces) {
        }

        /// <summary>
        /// Read raw node in the form of its attributes from remote.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="requestHeader"></param>
        /// <param name="nodeId"></param>
        /// <param name="skipValue"></param>
        /// <param name="operations"></param>
        /// <param name="traceOnly"></param>
        /// <returns></returns>
        public static async Task<RawNodeModel> ReadAsync(Session session,
            RequestHeader requestHeader, NodeId nodeId, bool skipValue,
            List<OperationResultModel> operations, bool traceOnly) {
            var node = new RawNodeModel(nodeId, session.NamespaceUris);
            await node.ReadAsync(session, requestHeader, skipValue, operations, traceOnly);
            return node;
        }

        /// <summary>
        /// Read value of a node
        /// </summary>
        /// <param name="session"></param>
        /// <param name="requestHeader"></param>
        /// <param name="nodeId"></param>
        /// <param name="operations"></param>
        /// <param name="traceOnly"></param>
        /// <returns></returns>
        public static async Task<Variant?> ReadValueAsync(Session session,
            RequestHeader requestHeader, NodeId nodeId,
            List<OperationResultModel> operations, bool traceOnly) {
            var node = new RawNodeModel(nodeId, session.NamespaceUris);
            await node.ReadValueAsync(session, requestHeader, operations, false, traceOnly);
            return node.Value;
        }

        /// <summary>
        /// Reads all node attributes through the passed in session from a remote
        /// server.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="requestHeader"></param>
        /// <param name="skipValue">Skip reading values for variables</param>
        /// <param name="operations"></param>
        /// <param name="traceOnly"></param>
        /// <returns></returns>
        public async Task ReadAsync(Session session, RequestHeader requestHeader,
            bool skipValue, List<OperationResultModel> operations, bool traceOnly) {
            var readValueCollection = new ReadValueIdCollection(_attributes.Keys
                .Where(a => !skipValue || a != Attributes.Value)
                .Select(a => new ReadValueId {
                    NodeId = LocalId,
                    AttributeId = a
                }));
            await ReadAsync(session, requestHeader, readValueCollection, operations,
                true, traceOnly);
            if (skipValue && NodeClass == NodeClass.VariableType) {
                // Read default value
                await ReadValueAsync(session, requestHeader, operations, true, traceOnly);
            }
        }

        /// <summary>
        /// Reads the value through the passed in session from a remote server.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="requestHeader"></param>
        /// <param name="operations"></param>
        /// <param name="skipAttributeIdInvalid"></param>
        /// <param name="traceOnly"></param>
        /// <returns></returns>
        public async Task<DataValue> ReadValueAsync(Session session,
            RequestHeader requestHeader, List<OperationResultModel> operations,
            bool skipAttributeIdInvalid, bool traceOnly) {
            var readValueCollection = new ReadValueIdCollection {
                new ReadValueId {
                    NodeId = LocalId,
                    AttributeId = Attributes.Value
                }
            };
            if (NodeClass == NodeClass.Unspecified) {
                readValueCollection.Add(new ReadValueId {
                    NodeId = LocalId,
                    AttributeId = Attributes.NodeClass
                });
            }
            // Update value
            await ReadAsync(session, requestHeader, readValueCollection, operations,
                skipAttributeIdInvalid, traceOnly);
            if (operations == null &&
                NodeClass != NodeClass.VariableType && NodeClass != NodeClass.Variable) {
                throw new InvalidOperationException(
                    "Node is not a variable or variable type node and does not have value");
            }
            return _attributes[Attributes.Value];
        }

        /// <summary>
        /// Read using value collection
        /// </summary>
        /// <param name="session"></param>
        /// <param name="requestHeader"></param>
        /// <param name="readValueCollection"></param>
        /// <param name="operations"></param>
        /// <param name="skipAttributeIdInvalid"></param>
        /// <param name="traceOnly"></param>
        /// <returns></returns>
        internal async Task ReadAsync(Session session, RequestHeader requestHeader,
            ReadValueIdCollection readValueCollection, List<OperationResultModel> operations,
            bool skipAttributeIdInvalid, bool traceOnly) {

            var readResponse = await session.ReadAsync(requestHeader, 0,
                TimestampsToReturn.Both, readValueCollection, CancellationToken.None);

            OperationResultEx.Validate("Read_" + LocalId, operations,
                readResponse.Results
                    .Select(v => skipAttributeIdInvalid &&
                        v.StatusCode == StatusCodes.BadAttributeIdInvalid ?
                            StatusCodes.Good : v.StatusCode),
                readResponse.DiagnosticInfos, readValueCollection
                    .Select(v => AttributeMap.GetBrowseName(v.AttributeId)), traceOnly);

            for (var i = 0; i < readValueCollection.Count; i++) {
                var attributeId = readValueCollection[i].AttributeId;
                if (readResponse.Results[i].StatusCode != StatusCodes.BadAttributeIdInvalid) {
                    _attributes[attributeId] = readResponse.Results[i];
                }
            }
        }

        /// <summary>
        /// Writes any changes through the session
        /// </summary>
        /// <param name="session"></param>
        /// <param name="requestHeader"></param>
        /// <param name="operations"></param>
        /// <param name="skipAttributeIdInvalid"></param>
        /// <param name="traceOnly"></param>
        /// <returns></returns>
        public async Task WriteAsync(Session session, RequestHeader requestHeader,
            List<OperationResultModel> operations, bool skipAttributeIdInvalid, bool traceOnly) {
            var writeValueCollection = new WriteValueCollection(_attributes
                .Where(a => a.Value != null)
                .Select(a => new WriteValue {
                    NodeId = LocalId,
                    AttributeId = a.Key,
                    Value = a.Value
                }));

            var writeResponse = await session.WriteAsync(requestHeader,
                writeValueCollection, CancellationToken.None);
            OperationResultEx.Validate("Write_" + LocalId, operations, writeResponse.Results
                    .Select(code => skipAttributeIdInvalid &&
                        code == StatusCodes.BadAttributeIdInvalid ? StatusCodes.Good : code),
                writeResponse.DiagnosticInfos,
                writeValueCollection
                    .Select(v => AttributeMap.GetBrowseName(v.AttributeId)), traceOnly);
        }
    }
}
