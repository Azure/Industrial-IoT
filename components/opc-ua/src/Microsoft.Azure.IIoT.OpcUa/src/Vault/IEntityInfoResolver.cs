// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Resolves an entity to entity information
    /// </summary>
    public interface IEntityInfoResolver {

        /// <summary>
        /// Find entity information for entity identifier.
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<EntityInfoModel> FindEntityAsync(string entityId,
            CancellationToken ct = default);
    }
}
