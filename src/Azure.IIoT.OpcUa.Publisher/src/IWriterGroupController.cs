// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Writer group controller
    /// </summary>
    public interface IWriterGroupController
    {
        /// <summary>
        /// Update writer group and block
        /// </summary>
        /// <param name="writerGroup"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask UpdateAsync(WriterGroupModel writerGroup,
            CancellationToken ct = default);

        /// <summary>
        /// Deletes the writer group
        /// </summary>
        /// <param name="removeState"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask DeleteAsync(bool removeState = true,
            CancellationToken ct = default);
    }
}
