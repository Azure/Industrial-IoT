// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http {
    using System.Threading.Tasks;

    /// <summary>
    /// Authenticates https requests
    /// </summary>
    public interface IHttpAuthHandler {

        /// <summary>
        /// Handle request
        /// </summary>
        /// <param name="request"></param>
        Task OnRequestAsync(IHttpRequest request);

        /// <summary>
        /// Handle response
        /// </summary>
        /// <param name="response"></param>
        Task OnResponseAsync(IHttpResponse response);
    }
}