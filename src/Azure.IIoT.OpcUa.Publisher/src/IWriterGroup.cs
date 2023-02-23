// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using System;

    /// <summary>
    /// Writer group
    /// </summary>
    public interface IWriterGroup : IDisposable
    {
        /// <summary>
        /// Resolve source
        /// </summary>
        IMessageSource Source { get; }
    }
}
