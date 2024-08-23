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
    /// Asset configuration services. Manages assets in the configuration
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IAssetConfiguration<T>
    {
        /// <summary>
        /// Create an asset from the entry and the configuration provided in the Web of
        /// Things Asset configuration file. The entry in the request object must contain
        /// a data set name which will be used as the asset name. The writer id can stay
        /// empty and will be the asset id on successful return. The server must support the
        /// WoT profile as per <see href="https://reference.opcfoundation.org/WoT/v100/docs/"/>.
        /// The asset will be created and the configuration updated to reference it. A wait
        /// time can be provided in the request to have the server settle after uploading
        /// the configuration.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ServiceResponse<PublishedNodesEntryModel>> CreateOrUpdateAssetAsync(
            PublishedNodeCreateAssetRequestModel<T> request, CancellationToken ct = default);

        /// <summary>
        /// Get a list of entries representing the assets in the server. This will not touch
        /// the configuration, it will obtain the list from the server. If the server does not
        /// support <see href="https://reference.opcfoundation.org/WoT/v100/docs/"/> the
        /// result will be empty.
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="header"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        IAsyncEnumerable<ServiceResponse<PublishedNodesEntryModel>> GetAllAssetsAsync(
            PublishedNodesEntryModel entry, RequestHeaderModel? header = null,
            CancellationToken ct = default);

        /// <summary>
        /// Delete the asset referenced by the entry. The entry in the request must contain
        /// the asset id to delete. The asset id is the data set writer id.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ServiceResultModel> DeleteAssetAsync(PublishedNodeDeleteAssetRequestModel request,
            CancellationToken ct = default);
    }
}
