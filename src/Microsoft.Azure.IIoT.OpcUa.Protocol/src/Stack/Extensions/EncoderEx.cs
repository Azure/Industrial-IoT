// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Extensions {
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Encoder extensions
    /// </summary>
    public static class EncoderEx {

        /// <summary>
        /// Write typed enumerated
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="encoder"></param>
        /// <param name="field"></param>
        /// <param name="enums"></param>
        /// <returns></returns>
        public static void WriteEnumeratedArray<T>(this IEncoder encoder, string field, T[] enums)
            where T : Enum => encoder.WriteEnumeratedArray(field, enums, typeof(T));

        /// <summary>
        /// Write encodeables
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="encoder"></param>
        /// <param name="field"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public static void WriteEncodeableArray<T>(this IEncoder encoder, string field,
            IEnumerable<T> values) where T : IEncodeable =>
            encoder.WriteEncodeableArray(field, values.Cast<IEncodeable>().ToArray(), typeof(T));

        /// <summary>
        /// Write encodeable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="encoder"></param>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static void WriteEncodeable<T>(this IEncoder encoder, string field, T value)
            where T : IEncodeable => encoder.WriteEncodeable(field, value, typeof(T));
    }
}
