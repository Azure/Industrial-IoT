// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua {
    using System;
    using System.Text;

    /// <summary>
    /// Type serializer services extensions
    /// </summary>
    public static class TypeSerializerEx {

        /// <summary>
        /// Encode string
        /// </summary>
        /// <param name="codec"></param>
        /// <param name="writer"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string Encode(this ITypeSerializer codec,
            Action<IEncoder> writer, Encoding encoding) {
            var result = codec.Encode(writer);
            if (result != null) {
                return (encoding ?? Encoding.UTF8).GetString(result);
            }
            return null;
        }

        /// <summary>
        /// Decode string
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="codec"></param>
        /// <param name="input"></param>
        /// <param name="reader"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static T Decode<T>(this ITypeSerializer codec,
            string input, Func<IDecoder, T> reader, Encoding encoding) {
            if (string.IsNullOrEmpty(input)) {
                throw new ArgumentNullException(nameof(input));
            }
            var buffer = (encoding ?? Encoding.UTF8).GetBytes(input);
            return codec.Decode(buffer, reader);
        }
    }
}
