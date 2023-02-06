// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher {
    using System;

    /// <summary>
    /// Writer group scope
    /// </summary>
    public interface IWriterGroupScope : IDisposable {

        /// <summary>
        /// Resolve writer group
        /// </summary>
        IWriterGroup WriterGroup { get; }
    }
}