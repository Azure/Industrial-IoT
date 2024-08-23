// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Configuration services using node services to explore and expand
    /// requests based on the server address space.
    /// </summary>
    public interface IConfigurationServices
    {
        /// <summary>
        /// Expand the provided entry into configuration entries and return them one
        /// by one with the error items last. The configuration is not updated but
        /// the resulting entries without error info can be added in a later call to
        /// the publisher configuration api.
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        IAsyncEnumerable<ServiceResponse<PublishedNodesEntryModel>> ExpandAsync(
            PublishedNodesEntryModel entry, PublishedNodeExpansionModel request,
            CancellationToken ct = default);

        /// <summary>
        /// Create one or more writer entries by expanding the provided entry into the
        /// configuration. The expanded items are added to the configuration.
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        IAsyncEnumerable<ServiceResponse<PublishedNodesEntryModel>> CreateOrUpdateAsync(
            PublishedNodesEntryModel entry, PublishedNodeExpansionModel request,
            CancellationToken ct = default);
    }
}
