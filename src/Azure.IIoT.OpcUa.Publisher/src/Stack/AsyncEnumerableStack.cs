// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Wraps a stack
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class AsyncEnumerableEnumerableStack<T> : AsyncEnumerableBase<T>
    {
        /// <inheritdoc/>
        public override bool HasMore => _ops.Count > 0;

        /// <inheritdoc/>
        public override void Reset()
        {
            _ops.Clear();
        }

        /// <inheritdoc/>
        public override async ValueTask<IEnumerable<T>> ExecuteAsync(ServiceCallContext context)
        {
            var func = _ops.Pop();
            var cur = _ops.Count;
            try
            {
                return await func.Invoke(context).ConfigureAwait(false);
            }
            catch
            {
                if (_ops.Count == cur)
                {
                    _ops.Push(func);
                }
                throw;
            }
        }

        /// <inheritdoc/>
        protected void Push(Func<ServiceCallContext, ValueTask<IEnumerable<T>>> value)
        {
            _ops.Push(value);
        }

        /// <inheritdoc/>
        protected override ValueTask<T> RunAsync(ServiceCallContext context)
        {
            throw new NotSupportedException();
        }

        private readonly Stack<Func<ServiceCallContext, ValueTask<IEnumerable<T>>>> _ops = new();
    }

    /// <summary>
    /// Wraps a stack
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class AsyncEnumerableStack<T> : AsyncEnumerableBase<T>
    {
        /// <inheritdoc/>
        public override bool HasMore => _ops.Count > 0;

        /// <inheritdoc/>
        public override void Reset()
        {
            _ops.Clear();
        }

        /// <inheritdoc/>
        protected void Push(Func<ServiceCallContext, ValueTask<T>> value)
        {
            _ops.Push(value);
        }

        /// <inheritdoc/>
        protected override async ValueTask<T> RunAsync(ServiceCallContext context)
        {
            var func = _ops.Pop();
            var cur = _ops.Count;
            try
            {
                return await func.Invoke(context).ConfigureAwait(false);
            }
            catch
            {
                if (_ops.Count == cur)
                {
                    _ops.Push(func);
                }
                throw;
            }
        }

        private readonly Stack<Func<ServiceCallContext, ValueTask<T>>> _ops = new();
    }
}
