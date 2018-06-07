// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Proxy {
    using System;
    using System.Reflection;

    public interface IProxyInterceptor {

        /// <summary>
        /// Returns whether the interceptor can be applied to
        /// the type and method
        /// </summary>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        bool CanIntercept(Type type, MethodInfo method);
    }
}