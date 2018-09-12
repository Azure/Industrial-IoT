// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua {
    using System;

    /// <summary>
    /// Type serializer services
    /// </summary>
    public interface ITypeSerializer {

        /// <summary>
        /// Text mime type
        /// </summary>
        string MimeType { get; }

        /// <summary>
        /// Decode string into type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        T Decode<T>(byte[] input, Func<IDecoder, T> reader);

        /// <summary>
        /// Encode to string
        /// </summary>
        /// <param name="writer"></param>
        /// <returns></returns>
        byte[] Encode(Action<IEncoder> writer);
    }
}
