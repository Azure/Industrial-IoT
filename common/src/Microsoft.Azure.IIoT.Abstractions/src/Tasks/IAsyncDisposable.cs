// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT {
    using System.Threading.Tasks;

    /// <summary>
    /// Async disposable
    /// </summary>
    public interface IAsyncDisposable {

        /// <summary>
        /// Dispose async
        /// </summary>
        /// <returns></returns>
        Task DisposeAsync();
    }
}
