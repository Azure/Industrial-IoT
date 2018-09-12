// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Http.Proxy {
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Proxy services
    /// </summary>
    public interface IProxy {

        /// <summary>
        /// Forward to another endpoint and fill
        /// in response with received result.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        Task ForwardAsync(HttpRequest request,
            HttpResponse response);
    }
}
