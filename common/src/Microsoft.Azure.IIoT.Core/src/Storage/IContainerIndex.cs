// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage {
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Index management on top of a container
    /// </summary>
    public interface IContainerIndex {

        /// <summary>
        /// Allocate index
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<uint> AllocateAsync(
            CancellationToken ct = default);

        /// <summary>
        /// Free index
        /// </summary>
        /// <param name="index"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task FreeAsync(uint index,
            CancellationToken ct = default);
    }
}