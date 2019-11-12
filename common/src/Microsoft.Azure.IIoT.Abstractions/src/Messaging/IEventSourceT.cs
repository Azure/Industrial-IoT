// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging {
    using System;

    /// <summary>
    /// Event source
    /// </summary>
    public interface IEventSource<T> {

        /// <summary>
        /// Subscriber
        /// </summary>
        ICallbackRegistration Subscriber { get; }

        /// <summary>
        /// Event
        /// </summary>
        event Action<T> Events;
    }
}
