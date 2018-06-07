// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Proxy {
    using System;
    using System.Threading.Tasks;

    public interface IProxyFactory {

        /// <summary>
        /// Add interceptor
        /// </summary>
        /// <param name="interceptor"></param>
        void AddInterceptor(IProxyInterceptor interceptor);

        /// <summary>
        /// Create proxy for target interface
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        object CreateProxy(object target, Type type,
            object mixin);

        /// <summary>
        /// Create proxy for target interface
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        T CreateProxyT<T>(T target,
            object mixin) where T : class;

        /// <summary>
        /// Create proxy for target interface
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        object CreateProxyAsContinuation(Task target, Type type,
            object mixin);

        /// <summary>
        /// Create proxy for target interface
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        Task<T> CreateProxyAsContinuationT<T>(Task<T> target,
            object mixin) where T : class;

        /// <summary>
        /// Remove interceptor
        /// </summary>
        /// <param name="interceptor"></param>
        void RemoveInterceptor(IProxyInterceptor interceptor);
    }
}