// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
#nullable enable
namespace Azure.IIoT.OpcUa.Publisher.Parser.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class TestParserContext : List<IdentifierMetaData>,
        IFilterParserContext
    {
        public Task<IEnumerable<IdentifierMetaData>> GetIdentifiersAsync(string typeId,
            CancellationToken ct)
        {
            return Task.FromResult(this.Where(id => id.TypeDefinitionId == typeId));
        }

        public bool TryGetNamespaceUri(uint index, out string? namespaceUri)
        {
            if (index == 0)
            {
                namespaceUri = string.Empty;
                return true;
            }
            namespaceUri = null;
            return false;
        }
    }
}
