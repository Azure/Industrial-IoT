// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.Common.Http {
    using System.Net;
    using System.Net.Http.Headers;

    public class HttpResponse : IHttpResponse {

        /// <summary>
        /// Status code
        /// </summary>
        public HttpStatusCode StatusCode { get; internal set; }

        /// <summary>
        /// Headers
        /// </summary>
        public HttpResponseHeaders Headers { get; internal set; }

        /// <summary>
        /// Content
        /// </summary>
        public string Content { get; internal set; }
    }
}
