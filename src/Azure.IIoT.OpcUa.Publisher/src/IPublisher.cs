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
    }
}
