// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Sdk.SignalR
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// User callback registration client
    /// </summary>
    public interface ICallbackRegistrar
    {
        /// <summary>
        /// Connection receiving events
        /// </summary>
        string? ConnectionId { get; }

        /// <summary>
        /// Register handler to handle a method call
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="thiz"></param>
        /// <param name="method"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        IDisposable Register(Func<object?[], object, Task> handler,
            object thiz, string method, Type[] arguments);

        /// <summary>
        /// Register action
        /// </summary>
        /// <typeparam name="T0"></typeparam>
        /// <param name="method"></param>
        /// <param name="action"></param>
        public IDisposable Register<T0>(
            string method, Func<T0?, Task> action)
        {
            return Register((args, _) =>
            {
                return action.Invoke(
                    (T0?)args[0]
                );
            }, this, method,
            [
                typeof(T0)
            ]);
        }
    }
}
