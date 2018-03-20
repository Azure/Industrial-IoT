// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.Common.Http {
    using System.Net;
    using System.Net.Http.Headers;

    public interface IHttpResponse {

        /// <summary>
        /// Response code
        /// </summary>
        HttpStatusCode StatusCode { get; }

        /// <summary>
        /// Response headers
        /// </summary>
        HttpResponseHeaders Headers { get; }

        /// <summary>
        /// Response content
        /// </summary>
        string Content { get; }
    }
}
