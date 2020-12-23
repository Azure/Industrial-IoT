// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging {

    /// <summary>
    /// Typed message data
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IMessageData<T> : IMessageData {

        /// <summary>
        /// Data value
        /// </summary>
        new T Value { get; }
    }
}