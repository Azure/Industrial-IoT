// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http {
    using System.Net;
    using System.Net.Http.Headers;

    /// <summary>
    /// Response interface
    /// </summary>
    public interface IHttpResponse {

        /// <summary>
        /// Id of the resource that responded
        /// </summary>
        string ResourceId { get; }

        /// <summary>
        /// Response code
        /// </summary>
        HttpStatusCode StatusCode { get; }

        /// <summary>
        /// Response headers
        /// </summary>
        HttpResponseHeaders Headers { get; }

        /// <summary>
        /// Response content headers
        /// </summary>
        HttpContentHeaders ContentHeaders { get; }

        /// <summary>
        /// Response content
        /// </summary>
        byte[] Content { get; }
    }
}
