// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Parser
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Extensions;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Parser context based on session filter
    /// </summary>
    public sealed class SessionParserContext : IFilterParserContext
    {
        /// <summary>
        /// Error occurred during validation
        /// </summary>
        public ServiceResultModel? ErrorInfo { get; private set; }

        /// <summary>
        /// Create context
        /// </summary>
        /// <param name="session"></param>
        /// <param name="header"></param>
        /// <param name="format"></param>
        public SessionParserContext(IOpcUaSession session, RequestHeader header,
            NamespaceFormat format = NamespaceFormat.Index)
        {
            _session = session;
            _header = header;
            _format = format;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<IdentifierMetaData>> GetIdentifiersAsync(
            string typeId, CancellationToken ct)
        {
            var map = new Dictionary<ImmutableRelativePath, InstanceDeclarationModel>();
            var declarations = new List<InstanceDeclarationModel>();
            var nodeId = typeId.ToNodeId(_session.MessageContext);
            var hierarchy = new List<(NodeId, ReferenceDescription)>();

            // If we failed before, fail again
            if (ErrorInfo != null)
            {
                return [];
            }

            await _session.CollectTypeHierarchyAsync(_header, nodeId, hierarchy,
                ct).ConfigureAwait(false);
            hierarchy.Reverse(); // Start from Root super type

            foreach (var (subType, superType) in hierarchy)
            {
                // Only request variables to resolve
                ErrorInfo = await _session.CollectInstanceDeclarationsAsync(
                    _header, (NodeId)superType.NodeId, null, declarations,
                    map, _format, Opc.Ua.NodeClass.Variable, ct).ConfigureAwait(false);
                if (ErrorInfo != null)
                {
                    break;
                }
            }
            if (ErrorInfo == null)
            {
                // Collect the variables of the selected type.
                ErrorInfo = await _session.CollectInstanceDeclarationsAsync(
                    _header, nodeId, null, declarations, map, _format,
                    Opc.Ua.NodeClass.Variable, ct).ConfigureAwait(false);
            }
            if (ErrorInfo != null)
            {
                return [];
            }
            return declarations
                .Where(declaration =>
                    declaration.RootTypeId != null &&
                    declaration.BrowsePath != null)
                .Select(declaration => new IdentifierMetaData(
                    declaration.RootTypeId!,
                    declaration.BrowsePath!,
                    declaration.DisplayPath ?? declaration.BrowseName!));
        }

        /// <inheritdoc/>
        public bool TryGetNamespaceUri(uint index, out string? namespaceUri)
        {
            namespaceUri = _session.MessageContext.NamespaceUris.GetString(index);
            return namespaceUri != null;
        }

        private readonly IOpcUaSession _session;
        private readonly RequestHeader _header;
        private readonly NamespaceFormat _format;
    }
}
