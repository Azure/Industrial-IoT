// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Parser
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Identifier metadata
    /// </summary>
    /// <param name="TypeDefinitionId"></param>
    /// <param name="BrowsePath"></param>
    /// <param name="DisplayName"></param>
    public record IdentifierMetaData(string TypeDefinitionId,
        IReadOnlyList<string> BrowsePath, string DisplayName);

    /// <summary>
    /// Event filter parser context
    /// </summary>
    public interface IFilterParserContext
    {
        /// <summary>
        /// Get valid identifiers
        /// </summary>
        /// <param name="typeId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IEnumerable<IdentifierMetaData>> GetIdentifiersAsync(
            string typeId, CancellationToken ct = default);

        /// <summary>
        /// Resolve namespace uri
        /// </summary>
        /// <param name="index"></param>
        /// <param name="namespaceUri"></param>
        /// <returns></returns>
        bool TryGetNamespaceUri(uint index, out string? namespaceUri);
    }
}
