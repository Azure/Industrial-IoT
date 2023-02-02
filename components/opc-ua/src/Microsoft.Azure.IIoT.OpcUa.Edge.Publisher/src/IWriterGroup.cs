// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher {
    using System;

    /// <summary>
    /// Writer group
    /// </summary>
    public interface IWriterGroup : IDisposable {

        /// <summary>
        /// Resolve source
        /// </summary>
        IMessageTrigger Source { get; }
    }
}