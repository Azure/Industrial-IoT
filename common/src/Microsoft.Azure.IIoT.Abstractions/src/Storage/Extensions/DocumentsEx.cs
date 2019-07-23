// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage {
    using Microsoft.Azure.IIoT.Exceptions;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Documents extensions
    /// </summary>
    public static class DocumentsEx {

        /// <summary>
        /// Gets an item.
        /// </summary>
        /// <param name="documents"></param>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static async Task<IDocumentInfo<T>> GetAsync<T>(
            this IDocuments documents, string id, CancellationToken ct = default,
            OperationOptions options = null) {
            var result = await documents.FindAsync<T>(id, ct, options);
            if (result == null) {
                throw new ResourceNotFoundException($"Resource {id} not found");
            }
            return result;
        }
    }
}
