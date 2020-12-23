// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.Serializers {
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.AspNetCore.Mvc.Formatters;
    using Microsoft.Net.Http.Headers;
    using System;
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;

    /// <summary>
    /// Input Formatter
    /// </summary>
    public class SerializerInputFormatter : InputFormatter {

        /// <summary>
        /// Create formatter
        /// </summary>
        /// <param name="serializer"></param>
        public SerializerInputFormatter(ISerializer serializer) {
            _serializer = serializer;
            SupportedMediaTypes.Add(new MediaTypeHeaderValue(serializer.MimeType));
        }

        /// <inheritdoc/>
        public override async Task<InputFormatterResult> ReadRequestBodyAsync(
            InputFormatterContext context) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            var request = context.HttpContext.Request;

            // read everything into a buffer, and then seek back to the beginning.
            var memoryThreshold = kDefaultMemoryThreshold;
            var contentLength = request.ContentLength.GetValueOrDefault();
            if (contentLength > 0 && contentLength < memoryThreshold) {
                memoryThreshold = (int)contentLength;
            }
            var memory = await request.Body.ReadAsMemoryAsync(memoryThreshold);
            var result = _serializer.Deserialize(memory, context.ModelType);
            return await InputFormatterResult.SuccessAsync(result);
        }

        /// <inheritdoc/>
        protected override bool CanReadType(Type type) {
            if (type == null) {
                throw new ArgumentException("Type cannot be null");
            }
            var typeInfo = type.GetTypeInfo();
            return !typeInfo.IsAbstract && !typeInfo.IsInterface && typeInfo.IsPublic;
        }

        /// <inheritdoc/>
        public override bool CanRead(InputFormatterContext context) {
            if (!base.CanRead(context)) {
                return false;
            }
            if (_serializer.ContentEncoding != null) {
                var requestContentType = context.HttpContext.Request.ContentType;
                var requestMediaType = requestContentType == null ? default :
                    new MediaType(requestContentType);
                if (requestMediaType.Charset.HasValue &&
                    requestMediaType.Encoding != null &&
                    _serializer.ContentEncoding.WebName != requestMediaType.Encoding.WebName) {
                    return false;
                }
            }
            return true;
        }

        private const int kDefaultMemoryThreshold = 1024 * 30;
        private readonly ISerializer _serializer;
    }
}