// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http {
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Client interface
    /// </summary>
    public interface IHttpClient {

        /// <summary>
        /// Create new request
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="resourceId"></param>
        /// <returns></returns>
        IHttpRequest NewRequest(Uri uri, string resourceId = null);

        /// <summary>
        /// Perform get
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IHttpResponse> GetAsync(IHttpRequest request,
            CancellationToken ct = default);

        /// <summary>
        /// Perform post
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IHttpResponse> PostAsync(IHttpRequest request,
            CancellationToken ct = default);

        /// <summary>
        /// Perform put
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IHttpResponse> PutAsync(IHttpRequest request,
            CancellationToken ct = default);

        /// <summary>
        /// Perform patch
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IHttpResponse> PatchAsync(IHttpRequest request,
            CancellationToken ct = default);

        /// <summary>
        /// Perform delete
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IHttpResponse> DeleteAsync(IHttpRequest request,
            CancellationToken ct = default);

        /// <summary>
        /// Perform head
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IHttpResponse> HeadAsync(IHttpRequest request,
            CancellationToken ct = default);

        /// <summary>
        /// Perform options
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IHttpResponse> OptionsAsync(IHttpRequest request,
            CancellationToken ct = default);
    }
}
