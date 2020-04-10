// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.IdentityServer4 {
    using Microsoft.Azure.IIoT.Exceptions;
    using global::IdentityServer4.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Client repo extensions
    /// </summary>
    public static class ClientRepositoryEx {

        /// <summary>
        /// Create or update
        /// </summary>
        /// <param name="repo"></param>
        /// <param name="client"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task CreateOrUpdateAsync(this IClientRepository repo,
            Client client, CancellationToken ct = default) {
            while (true) {
                try {
                    var (existing, etag) = await repo.GetAsync(client.ClientId, ct);
                    await repo.UpdateAsync(client, etag, ct);
                }
                catch (ResourceOutOfDateException) {
                    // Out of date get it again and update
                    continue;
                }
                catch (ResourceNotFoundException) {
                    try {
                        await repo.CreateAsync(client, ct);
                    }
                    catch (ConflictingResourceException) {
                        // Existing - try to update
                        continue;
                    }
                }
                break;
            }
        }
    }
}