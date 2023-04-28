// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Publisher.Models;

    /// <summary>
    /// Creates writer group scopes
    /// </summary>
    public interface IWriterGroupScopeFactory
    {
        /// <summary>
        /// Create a writer group container containing everything
        /// needed to resolve the writer group processing engine.
        /// </summary>
        /// <param name="writerGroup"></param>
        /// <returns></returns>
        IWriterGroupScope Create(WriterGroupModel writerGroup);
    }
}
