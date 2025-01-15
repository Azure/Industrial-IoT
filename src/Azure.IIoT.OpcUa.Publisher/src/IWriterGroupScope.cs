// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using System;

    /// <summary>
    /// Writer group scope
    /// </summary>
    public interface IWriterGroupScope : IDisposable
    {
        /// <summary>
        /// Resolve writer group
        /// </summary>
        IWriterGroupControl WriterGroup { get; }
    }
}
