// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage {
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Patch documents using json patch
    /// </summary>
    public interface IDocumentPatcher {

        /// <summary>
        /// Patch document using json patch operations
        /// </summary>
        /// <param name="id"></param>
        /// <param name="patches"></param>
        /// <returns></returns>
        Task PatchAsync(string id, IEnumerable<string> patches);
    }
}
