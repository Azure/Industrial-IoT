// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Configuration services using node services to explore and expand
    /// requests based on the server address space.
    /// </summary>
    public interface IConfigurationServices
    {
        /// <summary>
        /// Create one or more writer entries by expanding the provided
        /// entry into the configuration. As items are expanded they are
        /// returned one by one with the error items last. The items are
        /// also added to the configuration unless noUpdate is set to true.
        /// If noUpdate is set to true, the configuration is not updated
        /// but the resulting entries without error info can be added in
        /// a later call to the publisher configuration api.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="noUpdate"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        IAsyncEnumerable<ServiceResponse<PublishedNodesEntryModel>> ExpandAsync(
            PublishedNodeExpansionRequestModel request, bool noUpdate = false,
            CancellationToken ct = default);
    }
}
