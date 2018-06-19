// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Dynamic.Castle {
    using global::Castle.DynamicProxy;
    using System;
    using System.Diagnostics;
    using System.Reflection;

    /// <summary>
    /// Intercept entity query interface
    /// </summary>
    public abstract class BaseInterceptor : IProxyInterceptor, IInterceptor {

        public void Intercept(IInvocation invocation) {
            Debug.Assert(CanIntercept(invocation.TargetType, invocation.Method));
            InterceptInternal(invocation);
        }

        protected abstract void InterceptInternal(IInvocation invocation);

        /// <summary>
        /// Returns whether the interceptor can intercept the method
        /// and or type.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public abstract bool CanIntercept(Type type, MethodInfo method);
    }
}
