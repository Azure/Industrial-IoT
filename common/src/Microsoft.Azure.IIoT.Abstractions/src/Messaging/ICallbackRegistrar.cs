// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging {
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// User callback registration client
    /// </summary>
    public interface ICallbackRegistrar {

        /// <summary>
        /// Connection receiving events
        /// </summary>
        string ConnectionId { get; }

        /// <summary>
        /// Register handler to handle a method call
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="thiz"></param>
        /// <param name="method"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        IDisposable Register(Func<object[], object, Task> handler,
            object thiz, string method, Type[] arguments);
    }
}
