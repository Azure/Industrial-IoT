// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Serializers {
    using Microsoft.Azure.IIoT.Http;
    using System;
    using System.Buffers;
    using System.IO;

    /// <summary>
    /// Serializer extensions
    /// </summary>
    public static class SerializerEx {

        /// <summary>
        /// Serialize to byte array
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="o"></param>
        /// <param name="format"></param>
        public static ReadOnlySpan<byte> SerializeToBytes(
            this ISerializer serializer, object o,
            SerializeOption format = SerializeOption.None) {
            var writer = new ArrayBufferWriter<byte>();
            serializer.Serialize(writer, o, format);
            return writer.WrittenSpan;
        }

        /// <summary>
        /// Serialize to string
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="a"></param>
        public static ReadOnlySpan<byte> SerializeArrayToBytes(
            this ISerializer serializer, params object[] a) {
            return serializer.SerializeToBytes(a);
        }

        /// <summary>
        /// Serialize to string
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="o"></param>
        /// <param name="format"></param>
        public static string SerializeToString(this ISerializer serializer,
            object o, SerializeOption format = SerializeOption.None) {
            var span = serializer.SerializeToBytes(o, format);
            return serializer.ContentEncoding?.GetString(span)
                ?? Convert.ToBase64String(span);
        }

        /// <summary>
        /// Serialize to string
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="a"></param>
        public static string SerializeArrayToString(
            this ISerializer serializer, params object[] a) {
            return serializer.SerializeToString(a);
        }

        /// <summary>
        /// Set accept headers
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public static void SetAcceptHeaders(this ISerializer serializer,
            IHttpRequest request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            request.AddHeader("Accept", serializer.MimeType);
            if (serializer.ContentEncoding != null) {
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
            IHttpRequest request, object o) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            serializer.SetAcceptHeaders(request);
            request.SetByteArrayContent(serializer.SerializeToBytes(o).ToArray(),
                 serializer.MimeType, serializer.ContentEncoding);
        }

        /// <summary>
        /// Serialize to request
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="request"></param>
        /// <param name="a"></param>
        /// <returns></returns>
        public static void SerializeArrayToRequest(this ISerializer serializer,
            IHttpRequest request, params object[] a) {
            serializer.SerializeToRequest(request, a);
        }

        /// <summary>
        /// Serialize into indented string
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="o"></param>
        /// <returns></returns>
        public static string SerializePretty(
            this ISerializer serializer, object o) {
            return serializer.SerializeToString(o, SerializeOption.Indented);
        }

        /// <summary>
        /// Serialize into indented string
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="a"></param>
        /// <returns></returns>
        public static string SerializeArrayPretty(
            this ISerializer serializer, params object[] a) {
            return serializer.SerializeToString(a, SerializeOption.Indented);
        }

        /// <summary>
        /// Deserialize from string
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="str"></param>
        /// <param name="type"></param>
        /// <param name="schemaReader"></param>
        /// <returns></returns>
        public static object Deserialize(this ISerializer serializer,
            string str, Type type, TextReader schemaReader = null) {
            var buffer = serializer.ContentEncoding?.GetBytes(str)
                ?? Convert.FromBase64String(str);

            return serializer.Deserialize(buffer, type, schemaReader);
        }

        /// <summary>
        /// Deserialize from reader
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serializer"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static T Deserialize<T>(this ISerializer serializer,
            ReadOnlyMemory<byte> buffer) {
            return (T)serializer.Deserialize(buffer, typeof(T));
        }

        /// <summary>
        /// Deserialize from reader
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serializer"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static T Deserialize<T>(this ISerializer serializer,
            TextReader reader) {
            return serializer.Deserialize<T>(reader.ReadToEnd());
        }

        /// <summary>
        /// Deserialize from validating reader
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serializer"></param>
        /// <param name="reader"></param>
        /// <param name="schemaReader"></param>
        /// <returns></returns>
        public static T Deserialize<T>(this ISerializer serializer,
            TextReader reader,
            TextReader schemaReader) {

            // Desrialize and validate json content in a single read cycle.
            return serializer.Deserialize<T>(reader.ReadToEnd(), schemaReader);
        }

        /// <summary>
        /// Deserialize from string
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serializer"></param>
        /// <param name="json"></param>
        /// <param name="schemaReader"></param>
        /// <returns></returns>
        public static T Deserialize<T>(this ISerializer serializer,
            string json,
            TextReader schemaReader = null) {
            var typed = serializer.Deserialize(json, typeof(T), schemaReader);
            return typed == null ? default : (T)typed;
        }

        /// <summary>
        /// Deserialize from response
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serializer"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        public static T DeserializeResponse<T>(this ISerializer serializer,
            IHttpResponse response) {
            var typed = serializer.Deserialize(response.Content, typeof(T));
            return typed == null ? default : (T)typed;
        }

        /// <summary>
        /// Convert to token.
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="a"></param>
        /// <returns></returns>
        public static VariantValue FromArray(this ISerializer serializer,
            params object[] a) {
            return serializer.FromObject(a);
        }

        /// <summary>
        /// Parse string
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public static VariantValue Parse(this ISerializer serializer,
            string str) {
            var buffer = serializer.ContentEncoding?.GetBytes(str)
                ?? Convert.FromBase64String(str);
            return serializer.Parse(buffer);
        }

        /// <summary>
        /// Parse response
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        public static VariantValue ParseResponse(this ISerializer serializer,
            IHttpResponse response) {
            return serializer.Parse(response.Content);
        }
    }
}