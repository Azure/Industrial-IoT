// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module {
    using Microsoft.Azure.IIoT.Module.Models;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Chunk processor
    /// </summary>
    public interface IChunkMethodServer : IDisposable {

        /// <summary>
        /// Process chunk
        /// </summary>
        /// <param name="chunk"></param>
        /// <returns></returns>
        Task<MethodChunkModel> ProcessAsync(
            MethodChunkModel chunk);
    }
}
