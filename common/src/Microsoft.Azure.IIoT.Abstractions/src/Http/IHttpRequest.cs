// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http {
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;

    /// <summary>
    /// Request interface
    /// </summary>
    public interface IHttpRequest {

        /// <summary>
        /// Id of the target resource
        /// </summary>
        string ResourceId { get; }

        /// <summary>
        /// Uri target of the request
        /// </summary>
        Uri Uri { get; }

        /// <summary>
        /// Headers
        /// </summary>
        HttpRequestHeaders Headers { get; }

        /// <summary>
        /// Content
        /// </summary>
        HttpContent Content { get; set; }

        /// <summary>
        /// Options
        /// </summary>
        HttpRequestOptions Options { get; }
    }
}
