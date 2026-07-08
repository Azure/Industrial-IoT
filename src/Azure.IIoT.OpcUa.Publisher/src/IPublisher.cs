// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Publisher
    /// </summary>
    public interface IPublisher
    {
        /// <summary>
        /// Current version number of the configuration
        /// </summary>
        uint Version { get; }

        /// <summary>
        /// Last change time
        /// </summary>
        DateTimeOffset LastChange { get; }

        /// <summary>
        /// Gets a snapshot of the list of writer groups in publisher.
        /// </summary>
        ImmutableList<WriterGroupModel> WriterGroups { get; }

        /// <summary>
        /// Try update configuration
        /// </summary>
        /// <param name="writerGroups"></param>
        /// <returns></returns>
        bool TryUpdate(IEnumerable<WriterGroupModel> writerGroups);

        /// <summary>
        /// Update configuration
        /// </summary>
        /// <param name="writerGroups"></param>
        /// <returns></returns>
        Task UpdateAsync(IEnumerable<WriterGroupModel> writerGroups);

        /// <summary>
        /// Force sending a key frame message immediately for a writer group,
        /// or for a specific data set writer within the group. A key frame
        /// contains a snapshot of all currently cached values which allows
        /// late joining consumers to obtain the current state on demand
        /// without waiting for the next value change or the configured key
        /// frame interval.
        /// </summary>
        /// <param name="writerGroupId">The writer group to send key frames
        /// for.</param>
        /// <param name="dataSetWriterId">The data set writer to send the key
        /// frame for, or <c>null</c> for all writers in the group.</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask SendKeyFrameAsync(string writerGroupId,
            string? dataSetWriterId, CancellationToken ct);

        /// <summary>
        /// Get current state of a writer group by identifier
        /// </summary>
        /// <param name="writerGroupId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<WriterGroupStateDiagnosticModel> GetStateAsync(
            string writerGroupId, CancellationToken ct);

        /// <summary>
        /// Get the current state of all writer groups managed by the
        /// publisher. This includes the errors for all nodes that could
        /// not be created as monitored items across all endpoints.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<IReadOnlyList<WriterGroupStateDiagnosticModel>> GetStateAsync(
            CancellationToken ct);
    }
}
