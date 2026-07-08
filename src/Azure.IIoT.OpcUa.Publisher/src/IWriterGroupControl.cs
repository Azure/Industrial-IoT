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
    public interface IWriterGroupControl
    {

        /// <summary>
        /// Start group
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask StartAsync(CancellationToken ct);

        /// <summary>
        /// Update group
        /// </summary>
        /// <param name="writerGroup"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask UpdateAsync(WriterGroupModel writerGroup,
            CancellationToken ct);

        /// <summary>
        /// Force sending a key frame message immediately for all data set
        /// writers in the group, or for a specific data set writer if
        /// <paramref name="dataSetWriterId"/> is provided. A key frame
        /// contains a snapshot of all currently cached values.
        /// </summary>
        /// <param name="dataSetWriterId">The data set writer to send the key
        /// frame for, or <c>null</c> for all writers in the group.</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask SendKeyFrameAsync(string? dataSetWriterId,
            CancellationToken ct);

        /// <summary>
        /// Get state diagnostic information
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<WriterGroupStateDiagnosticModel> GetStateAsync(
            CancellationToken ct);
    }
}
