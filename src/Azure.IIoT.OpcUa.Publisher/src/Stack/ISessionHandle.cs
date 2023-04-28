// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using System;

    /// <summary>
    /// A session handle under lock.
    /// </summary>
    public interface ISessionHandle : IDisposable
    {
        /// <summary>
        /// Session handle
        /// </summary>
        IOpcUaSession Handle { get; }
    }
}
