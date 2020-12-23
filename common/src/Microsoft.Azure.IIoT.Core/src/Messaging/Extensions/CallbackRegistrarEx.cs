// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging {
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Callback Registrar extensions
    /// </summary>
    public static class CallbackRegistrarEx {

        /// <summary>
        /// Register action
        /// </summary>
        /// <param name="registrar"></param>
        /// <param name="method"></param>
        /// <param name="action"></param>
        public static IDisposable Register<T0>(this ICallbackRegistrar registrar,
            string method, Action<T0> action) {
            return registrar.Register((args, _) => {
                action.Invoke(
                    (T0)args[0]
                );
                return Task.CompletedTask;
            }, registrar, method,
            new Type[] {
                typeof(T0)
            });
        }

        /// <summary>
        /// Register action
        /// </summary>
        /// <param name="registrar"></param>
        /// <param name="method"></param>
        /// <param name="action"></param>
        public static IDisposable Register<T0, T1>(this ICallbackRegistrar registrar,
            string method, Action<T0, T1> action) {
            return registrar.Register((args, _) => {
                action.Invoke(
                    (T0)args[0],
                    (T1)args[1]
                );
                return Task.CompletedTask;
            }, registrar, method,
            new Type[] {
                typeof(T0),
                typeof(T1)
            });
        }

        /// <summary>
        /// Register action
        /// </summary>
        /// <param name="registrar"></param>
        /// <param name="method"></param>
        /// <param name="action"></param>
        public static IDisposable Register<T0, T1, T2>(this ICallbackRegistrar registrar,
            string method, Action<T0, T1, T2> action) {
            return registrar.Register((args, _) => {
                action.Invoke(
                    (T0)args[0],
                    (T1)args[1],
                    (T2)args[2]
                );
                return Task.CompletedTask;
            }, registrar, method,
            new Type[] {
                typeof(T0),
                typeof(T1),
                typeof(T2)
            });
        }

        /// <summary>
        /// Register action
        /// </summary>
        /// <param name="registrar"></param>
        /// <param name="method"></param>
        /// <param name="action"></param>
        public static IDisposable Register<T0, T1, T2, T3>(this ICallbackRegistrar registrar,
            string method, Action<T0, T1, T2, T3> action) {
            return registrar.Register((args, _) => {
                action.Invoke(
                    (T0)args[0],
                    (T1)args[1],
                    (T2)args[2],
                    (T3)args[3]
                );
                return Task.CompletedTask;
            }, registrar, method,
            new Type[] {
                typeof(T0),
                typeof(T1),
                typeof(T2),
                typeof(T3)
            });
        }

        /// <summary>
        /// Register action
        /// </summary>
        /// <param name="registrar"></param>
        /// <param name="method"></param>
        /// <param name="action"></param>
        public static IDisposable Register<T0>(this ICallbackRegistrar registrar,
            string method, Func<T0, Task> action) {
            return registrar.Register((args, _) => {
                return action.Invoke(
                    (T0)args[0]
                );
            }, registrar, method,
            new Type[] {
                typeof(T0)
            });
        }

        /// <summary>
        /// Register action
        /// </summary>
        /// <param name="registrar"></param>
        /// <param name="method"></param>
        /// <param name="action"></param>
        public static IDisposable Register<T0, T1>(this ICallbackRegistrar registrar,
            string method, Func<T0, T1, Task> action) {
            return registrar.Register((args, _) => {
                return action.Invoke(
                    (T0)args[0],
                    (T1)args[1]
                );
            }, registrar, method,
            new Type[] {
                typeof(T0),
                typeof(T1)
            });
        }

        /// <summary>
        /// Register action
        /// </summary>
        /// <param name="registrar"></param>
        /// <param name="method"></param>
        /// <param name="action"></param>
        public static IDisposable Register<T0, T1, T2>(this ICallbackRegistrar registrar,
            string method, Func<T0, T1, T2, Task> action) {
            return registrar.Register((args, _) => {
                return action.Invoke(
                    (T0)args[0],
                    (T1)args[1],
                    (T2)args[2]
                );
            }, registrar, method,
            new Type[] {
                typeof(T0),
                typeof(T1),
                typeof(T2)
            });
        }

        /// <summary>
        /// Register action
        /// </summary>
        /// <param name="registrar"></param>
        /// <param name="method"></param>
        /// <param name="action"></param>
        public static IDisposable Register<T0, T1, T2, T3>(this ICallbackRegistrar registrar,
            string method, Func<T0, T1, T2, T3, Task> action) {
            return registrar.Register((args, _) => {
                return action.Invoke(
                    (T0)args[0],
                    (T1)args[1],
                    (T2)args[2],
                    (T3)args[3]
                );
            }, registrar, method,
            new Type[] {
                typeof(T0),
                typeof(T1),
                typeof(T2),
                typeof(T3)
            });
        }
    }
}
