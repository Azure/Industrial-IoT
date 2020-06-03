// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using Microsoft.Azure.IIoT.Serializers;
    using System;
    using System.IO;
    using System.IO.Compression;

    /// <summary>
    /// Query continuation token extensions
    /// </summary>
    public static class QueryContinuationEx {

        /// <summary>
        /// Convert to continuation token string
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="continuation"></param>
        /// <returns></returns>
        public static string SerializeContinuationToken(this ISerializer serializer,
            QueryContinuation continuation) {
            using (var result = new MemoryStream()) {
                using (var gs = new GZipStream(result, CompressionMode.Compress)) {
                    gs.Write(serializer.SerializeToBytes(continuation));
                }
                return result.ToArray().ToBase64String();
            }
        }

        /// <summary>
        /// Convert to continuation token
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="query"></param>
        /// <param name="continuationToken"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public static string SerializeContinuationToken(this ISerializer serializer,
            string query, string continuationToken, int? pageSize) {
            return SerializeContinuationToken(serializer, new QueryContinuation {
                PageSize = pageSize,
                Query = query,
                Token = continuationToken
            });
        }

        /// <summary>
        /// Convert to continuation
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="continuationToken"></param>
        /// <returns></returns>
        public static QueryContinuation DeserializeContinuationToken(this ISerializer serializer,
            string continuationToken) {
            try {
                using (var input = new MemoryStream(continuationToken.DecodeAsBase64()))
                using (var gs = new GZipStream(input, CompressionMode.Decompress))
                using (var reader = new StreamReader(gs)) {
                    return serializer.Deserialize<QueryContinuation>(reader);
                }
            }
            catch (Exception ex) {
                throw new ArgumentException("Malformed continuation token",
                    nameof(continuationToken), ex);
            }
        }

        /// <summary>
        /// Convert to continuation
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="token"></param>
        /// <param name="continuationToken"></param>
        /// <param name="pageSize"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public static void DeserializeContinuationToken(this ISerializer serializer,
            string token, out string query, out string continuationToken, out int? pageSize) {
            var result = DeserializeContinuationToken(serializer, token);
            query = result.Query; continuationToken = result.Token; pageSize = result.PageSize;
        }
    }
}
