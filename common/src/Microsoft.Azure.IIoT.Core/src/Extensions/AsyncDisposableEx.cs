// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Utils {
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Async disposable extensions
    /// </summary>
    public static class AsyncDisposableEx {

        /// <summary>
        /// Safe waiting for disposables
        /// </summary>
        /// <param name="disposables"></param>
        /// <returns></returns>
        public static Task DisposeAsync(
            this IEnumerable<IAsyncDisposable> disposables) {
            return AsyncDisposable.DisposeAsync(disposables);
        }

        /// <summary>
        /// Create from tasks
        /// </summary>
        /// <param name="tasks"></param>
        /// <returns></returns>
#pragma warning disable IDE1006 // Naming Styles
        public static async Task<IAsyncDisposable> AsAsyncDisposable(
            this IEnumerable<Task<IAsyncDisposable>> tasks) {
#pragma warning restore IDE1006 // Naming Styles
            return new AsyncDisposable(await AsyncDisposable.WhenAll(tasks));
        }

        /// <summary>
        /// Create from tasks
        /// </summary>
        /// <param name="tasks"></param>
        /// <returns></returns>
#pragma warning disable IDE1006 // Naming Styles
        public static async Task<IDisposable> ToDisposable(
            this IEnumerable<Task<IAsyncDisposable>> tasks) {
#pragma warning restore IDE1006 // Naming Styles
            return new DisposableAdapter(
                new AsyncDisposable(await AsyncDisposable.WhenAll(tasks)));
        }

        /// <summary>
        /// Safe waiting for disposables
        /// </summary>
        /// <param name="disposables"></param>
        /// <returns></returns>
        public static IDisposable ToDisposable(
            this IEnumerable<IAsyncDisposable> disposables) {
            return new DisposableAdapter(new AsyncDisposable(disposables));
        }

        /// <summary>
        /// Disposable adapter class
        /// </summary>
        private sealed class DisposableAdapter : IDisposable {

            /// <inheritdoc/>
            public DisposableAdapter(IAsyncDisposable asyncDisposable) {
                _asyncDisposable = asyncDisposable;
            }

            /// <inheritdoc/>
            public void Dispose() {
                _asyncDisposable.DisposeAsync().AsTask().Wait();
            }

            private readonly IAsyncDisposable _asyncDisposable;
        }
    }
}
