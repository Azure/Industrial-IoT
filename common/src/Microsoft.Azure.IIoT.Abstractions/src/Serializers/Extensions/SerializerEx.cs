// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Abstractions.Serializers.Extensions
{
    using Furly.Extensions.Serializers;
    using Microsoft.Azure.IIoT.Abstractions.Serializers.Extensions;
    using Microsoft.Azure.IIoT.Http;
    using System;
    using System.IO;

    /// <summary>
    /// Serializer extensions
    /// </summary>
    public static class SerializerEx
    {
        /// <summary>
        /// Set accept headers
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public static void SetAcceptHeaders(this ISerializer serializer,
            IHttpRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            request.AddHeader("Accept", serializer.MimeType);
            if (serializer.ContentEncoding != null)
            {
                request.AddHeader("Accept-Charset", serializer.ContentEncoding.WebName);
            }
        }

        /// <summary>
        /// Serialize to request
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="request"></param>
        /// <param name="o"></param>
        /// <returns></returns>
        public static void SerializeToRequest(this ISerializer serializer,
            IHttpRequest request, object o)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            serializer.SetAcceptHeaders(request);
            request.SetByteArrayContent(serializer.SerializeToMemory(o).ToArray(),
                 serializer.MimeType, serializer.ContentEncoding);
        }

        /// <summary>
        /// Deserialize from response
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serializer"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        public static T DeserializeResponse<T>(this ISerializer serializer,
            IHttpResponse response)
        {
            var typed = serializer.Deserialize(response.Content, typeof(T));
            return typed == null ? default : (T)typed;
        }

        /// <summary>
        /// Parse response
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        public static VariantValue ParseResponse(this ISerializer serializer,
            IHttpResponse response)
        {
            return serializer.Parse(response.Content);
        }
    }
}
