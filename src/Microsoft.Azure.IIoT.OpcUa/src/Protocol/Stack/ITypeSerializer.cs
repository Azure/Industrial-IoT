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
        /// Decode bytes into type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="contentType"></param>
        /// <param name="input"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        T Decode<T>(string contentType, byte[] input,
            Func<IDecoder, T> reader);

        /// <summary>
        /// Encode to bytes
        /// </summary>
        /// <param name="contentType"></param>
        /// <param name="writer"></param>
        /// <returns></returns>
        byte[] Encode(string contentType,
            Action<IEncoder> writer);
    }
}
