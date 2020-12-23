// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.Serializers {
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.AspNetCore.Mvc.Formatters;
    using Microsoft.Net.Http.Headers;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Output formatter
    /// </summary>
    public class SerializerOutputFormatter : OutputFormatter {

        /// <summary>
        /// Create formatter
        /// </summary>
        /// <param name="serializer"></param>
        public SerializerOutputFormatter(ISerializer serializer) {
            _serializer = serializer;
            SupportedMediaTypes.Add(new MediaTypeHeaderValue(serializer.MimeType));
        }

        /// <inheritdoc/>
        public override async Task WriteResponseBodyAsync(
            OutputFormatterWriteContext context) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            await context.HttpContext.Response.Body.WriteAsync(
                _serializer.SerializeToBytes(context.Object).ToArray());
        }

        private readonly ISerializer _serializer;
    }
}