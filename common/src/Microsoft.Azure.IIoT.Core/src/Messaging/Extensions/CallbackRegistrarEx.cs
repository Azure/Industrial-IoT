// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Callback Registrar extensions
    /// </summary>
    public static class CallbackRegistrarEx
    {
        /// <summary>
        /// Register action
        /// </summary>
        /// <typeparam name="T0"></typeparam>
        /// <param name="registrar"></param>
        /// <param name="method"></param>
        /// <param name="action"></param>
        public static IDisposable Register<T0>(this ICallbackRegistrar registrar,
            string method, Func<T0, Task> action)
        {
            return registrar.Register((args, _) =>
            {
                return action.Invoke(
                    (T0)args[0]
                );
            }, registrar, method,
            new Type[] {
                typeof(T0)
            });
        }
    }
}
