// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher {
    using Microsoft.Azure.IIoT.OpcUa.Publisher;

    /// <summary>
    /// Creates writer group scopes
    /// </summary>
    public interface IWriterGroupScopeFactory {

        /// <summary>
        /// Create a writer group container containing everything
        /// needed to resolve the writer group processing engine.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        IWriterGroupScope Create(IWriterGroupConfig config);
    }
}