// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Linq;

    /// <summary>
    /// Async enumerable operation
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    public abstract class AsyncEnumerableBase<TResult>
    {
        /// <summary>
        /// Returns whether the operation is completed
        /// </summary>
        public abstract bool HasMore { get; }

        /// <summary>
        /// Reset the enumeration
        /// </summary>
        public abstract void Reset();

        /// <summary>
        /// Execute
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public virtual async ValueTask<IEnumerable<TResult>> ExecuteAsync(
            ServiceCallContext context)
        {
            var result = await RunAsync(context).ConfigureAwait(false);
            return result.YieldReturn();
        }

        /// <summary>
        /// Execute
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected abstract ValueTask<TResult> RunAsync(ServiceCallContext context);
    }
}
