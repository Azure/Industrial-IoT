// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Extensions {
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Decoder extensions
    /// </summary>
    public static class DecoderEx {

        /// <summary>
        /// Read typed enumerated
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="decoder"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static T ReadEnumerated<T>(this IDecoder decoder, string field)
            where T : Enum {
            return (T)decoder.ReadEnumerated(field, typeof(T));
        }

        /// <summary>
        /// Read typed enumerated
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="decoder"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static T[] ReadEnumeratedArray<T>(this IDecoder decoder, string field)
            where T : Enum {
            return (T[])decoder.ReadEnumeratedArray(field, typeof(T));
        }

        /// <summary>
        /// Read encodeables
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="decoder"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static IEnumerable<T> ReadEncodeableArray<T>(this IDecoder decoder, string field)
            where T : IEncodeable {
            return (IEnumerable<T>)decoder.ReadEncodeableArray(field, typeof(T));
        }

        /// <summary>
        /// Read encodeable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="decoder"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static T ReadEncodeable<T>(this IDecoder decoder, string field)
            where T : IEncodeable {
            return (T)decoder.ReadEncodeable(field, typeof(T));
        }
    }
}
