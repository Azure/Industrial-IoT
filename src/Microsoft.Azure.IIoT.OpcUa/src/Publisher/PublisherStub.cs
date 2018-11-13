// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Stubbed out publisher functionality
    /// </summary>
    public class PublisherStub : IPublishServices<EndpointModel> {

        /// <inheritdoc/>
        public Task<PublishedNodeListResultModel> NodePublishListAsync(
            EndpointModel endpoint, PublishedNodeListRequestModel request) {
            return Task.FromResult(new PublishedNodeListResultModel());
        }

        /// <inheritdoc/>
        public Task<PublishStartResultModel> NodePublishStartAsync(
            EndpointModel endpoint, PublishStartRequestModel request) {
            throw new NotSupportedException("Publishing not supported");
        }

        /// <inheritdoc/>
        public Task<PublishStopResultModel> NodePublishStopAsync(
            EndpointModel endpoint, PublishStopRequestModel request) {
            return Task.FromResult(new PublishStopResultModel());
        }
    }
}
