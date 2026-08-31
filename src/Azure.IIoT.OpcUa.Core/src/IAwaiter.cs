// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core
{
    using System.Runtime.CompilerServices;

    /// <summary>
    /// The awaiter for the object
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IAwaiter<out T> : INotifyCompletion
    {
        /// <summary>
        /// Is completed
        /// </summary>
        bool IsCompleted { get; }

        /// <summary>
        /// Returns the result
        /// </summary>
        T GetResult();
    }
}
