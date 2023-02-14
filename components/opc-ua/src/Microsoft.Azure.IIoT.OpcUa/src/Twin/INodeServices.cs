// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Node services via endpoint model
    /// </summary>
    public interface INodeServices<T> {

        /// <summary>
        /// Read node value
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ValueReadResultModel> NodeValueReadAsync(T endpoint,
            ValueReadRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Write node value
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ValueWriteResultModel> NodeValueWriteAsync(T endpoint,
            ValueWriteRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Get meta data for method call (input and output arguments)
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<MethodMetadataResultModel> NodeMethodGetMetadataAsync(
            T endpoint, MethodMetadataRequestModel request,
            CancellationToken ct = default);

        /// <summary>
        /// Call method
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<MethodCallResultModel> NodeMethodCallAsync(T endpoint,
            MethodCallRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Read node attributes in batch
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ReadResultModel> NodeReadAsync(T endpoint,
            ReadRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Write node attributes in batch
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<WriteResultModel> NodeWriteAsync(T endpoint,
            WriteRequestModel request, CancellationToken ct = default);
    }
}
