// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Utils
{
    using Furly.Extensions.Utils;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Helper to dispose a disposable and run cleanup
    /// </summary>
    public sealed class AsyncDisposable : IAsyncDisposable
    {
        /// <summary>
        /// Create disposable
        /// </summary>
        /// <param name="disposable"></param>
        /// <param name="disposeAsync"></param>
        public AsyncDisposable(IDisposable disposable = null,
            Func<Task> disposeAsync = null)
        {
            _disposable = disposable;
            if (disposeAsync != null)
            {
                _disposeAsync = () => Try.Async(() => disposeAsync.Invoke());
            }
            else
            {
                _disposeAsync = () => Task.CompletedTask;
            }
        }

        /// <summary>
        /// Create disposable
        /// </summary>
        /// <param name="disposables"></param>
        public AsyncDisposable(IEnumerable<IAsyncDisposable> disposables)
        {
            _disposeAsync = () => DisposeAsync(disposables);
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (_disposable != null)
            {
                Try.Op(_disposable.Dispose);
            }
            if (_disposeAsync != null)
            {
                await _disposeAsync.Invoke().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Safe waiting for disposables
        /// </summary>
        /// <param name="tasks"></param>
        /// <returns></returns>
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable AMNF0001 // Asynchronous method name is not ending with 'Async'
        public static async Task<IAsyncDisposable[]> WhenAll(
#pragma warning restore AMNF0001 // Asynchronous method name is not ending with 'Async'
            IEnumerable<Task<IAsyncDisposable>> tasks)
        {
#pragma warning restore IDE1006 // Naming Styles
            try
            {
                return await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch
            {
                foreach (var task in tasks)
                {
                    if (task.IsCompleted)
                    {
                        await Try.Async(() => task.Result.DisposeAsync().AsTask()).ConfigureAwait(false);
                    }
                }
                throw;
            }
        }

        /// <summary>
        /// Safe waiting for disposables
        /// </summary>
        /// <param name="disposables"></param>
        /// <returns></returns>
        public static Task DisposeAsync(IEnumerable<IAsyncDisposable> disposables)
        {
            return Try.Async(() => Task.WhenAll(disposables
                .Where(d => d != null)
                .Select(d => d.DisposeAsync().AsTask())));
        }

        private readonly IDisposable _disposable;
        private readonly Func<Task> _disposeAsync;
    }
}
