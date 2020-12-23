// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging {
    using System.Threading.Tasks;

    /// <summary>
    /// Handles typed integration events
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IEventHandler<T> : IHandler {

        /// <summary>
        /// Handle event
        /// </summary>
        /// <param name="eventData"></param>
        /// <returns></returns>
        Task HandleAsync(T eventData);
    }
}
