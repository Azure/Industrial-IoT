// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher {
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using Autofac;

    /// <summary>
    /// Creates writer group containers
    /// </summary>
    public interface IWriterGroupContainerFactory {

        /// <summary>
        /// Publisher identity
        /// </summary>
        string PublisherId { get; }

        /// <summary>
        /// Create a writer group container containing everything
        /// needed to resolve the writer group processing engine.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        IWriterGroup CreateWriterGroupScope(IWriterGroupConfig config);
    }
}