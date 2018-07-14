// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MsgPack {
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Abstract base class for type serializers
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Serializer<T> {

        /// <summary>
        /// Read an object
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public abstract Task<T> ReadAsync(Reader reader,
            SerializerContext context, CancellationToken ct);

        /// <summary>
        /// Write an object
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="obj"></param>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public abstract Task WriteAsync(Writer writer, T obj,
            SerializerContext context, CancellationToken ct);

        public async Task<object> GetAsync(Reader reader,
            SerializerContext context, CancellationToken ct) =>
            await ReadAsync(reader, context, ct).ConfigureAwait(false);

        public Task SetAsync(Writer writer, object obj,
            SerializerContext context, CancellationToken ct) =>
            WriteAsync(writer, (T)obj, context, ct);
    }
}