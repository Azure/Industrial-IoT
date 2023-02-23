// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Async disposable extensions
    /// </summary>
    public static class AsyncDisposableEx
    {
        /// <summary>
        /// Create from tasks
        /// </summary>
        /// <param name="tasks"></param>
        /// <returns></returns>
#pragma warning disable IDE1006 // Naming Styles
        public static async Task<IAsyncDisposable> AsAsyncDisposable(
            this IEnumerable<Task<IAsyncDisposable>> tasks)
        {
#pragma warning restore IDE1006 // Naming Styles
            return new AsyncDisposable(await AsyncDisposable.WhenAll(tasks).ConfigureAwait(false));
        }
    }
}
