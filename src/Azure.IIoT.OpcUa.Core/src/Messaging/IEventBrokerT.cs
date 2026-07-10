// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Registry event broker
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IEventBroker<T> where T : class
    {
        /// <summary>
        /// Notify all listeners
        /// </summary>
        /// <param name="evt"></param>
        Task NotifyAllAsync(Func<T, Task> evt);

        /// <summary>
        /// Notify all listeners
        /// </summary>
        /// <param name="evt"></param>
        void NotifyAll(Func<T, Task> evt);
    }
}
