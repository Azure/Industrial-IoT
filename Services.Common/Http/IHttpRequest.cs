// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.Common.Http {
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;

    /// <summary>
    /// Request interface
    /// </summary>
    public interface IHttpRequest {

        /// <summary>
        /// Target
        /// </summary>
        Uri Uri { get; set; }

        /// <summary>
        /// Headers
        /// </summary>
        HttpHeaders Headers { get; }

        /// <summary>
        /// Content type
        /// </summary>
        MediaTypeHeaderValue ContentType { get; }

        /// <summary>
        /// Options
        /// </summary>
        HttpRequestOptions Options { get; }

        /// <summary>
        /// Content
        /// </summary>
        HttpContent Content { get; }

        /// <summary>
        /// Add header
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        void AddHeader(string name, string value);

        /// <summary>
        /// Set content
        /// </summary>
        /// <param name="content"></param>
        /// <param name="encoding"></param>
        /// <param name="mediaType"></param>
        void SetContent(string content, Encoding encoding,
            MediaTypeHeaderValue mediaType);

        /// <summary>
        /// Set content
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sourceObject"></param>
        /// <param name="encoding"></param>
        /// <param name="mediaType"></param>
        void SetContent<T>(T sourceObject, Encoding encoding,
            MediaTypeHeaderValue mediaType);
    }
}
