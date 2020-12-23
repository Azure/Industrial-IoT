// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Default {
    using Microsoft.Azure.IIoT.Module;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    /// <summary>
    /// Adapts invokers to a method handler.  For real world usage use
    /// method router in module framework project.
    /// </summary>
    public class MethodHandlerAdapter : IMethodHandler {

        /// <summary>
        /// Create adapter
        /// </summary>
        /// <param name="invokers"></param>
        public MethodHandlerAdapter(IEnumerable<IMethodInvoker> invokers) {
            _calltable = invokers?.ToDictionary(i => i.MethodName, i => i) ??
              throw new ArgumentNullException(nameof(invokers));
        }

        /// <inheritdoc/>
        public Task<byte[]> InvokeAsync(string method, byte[] payload,
            string contentType) {
            if (_calltable.TryGetValue(method, out var invoker)) {
                return invoker.InvokeAsync(payload, contentType, this);
            }
            return Task.FromException<byte[]>(
                new InvalidOperationException($"{method} invoker not registered"));
        }

        private readonly Dictionary<string, IMethodInvoker> _calltable;
    }
}
