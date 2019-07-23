// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin.Default {
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Stubbed out publishing functionality
    /// </summary>
    public class PublishServicesStub<T> : IPublishServices<T> {

        /// <inheritdoc/>
        public Task<PublishedItemListResultModel> NodePublishListAsync(
            T endpoint, PublishedItemListRequestModel request) {
            return Task.FromResult(new PublishedItemListResultModel());
        }

        /// <inheritdoc/>
        public Task<PublishStartResultModel> NodePublishStartAsync(
            T endpoint, PublishStartRequestModel request) {
            throw new NotSupportedException("Publishing not supported");
        }

        /// <inheritdoc/>
        public Task<PublishStopResultModel> NodePublishStopAsync(
            T endpoint, PublishStopRequestModel request) {
            return Task.FromResult(new PublishStopResultModel());
        }
    }
}
